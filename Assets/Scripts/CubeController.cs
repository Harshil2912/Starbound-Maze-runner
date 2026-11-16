using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
public class CubeController : MonoBehaviour
{
    public const string MoveModePrefKey = "MR_MoveStickyMode";

    public float moveSpeed = 6f;

    // Jump tuning
    public float jumpSpeed = 7f;
    public float fallGravityMultiplier = 1.6f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;

    [Header("AR Mode Jump Tuning")]
    public float arJumpVelocityMultiplier = 1.02f;
    public float arFallGravityMultiplier = 1.45f;

    // If the cube falls below this height it goes back to the last respawn point.
    public float fallY = -9f;
    public bool fallsCostHealth;

    public bool stickyMode = true;
    public ParticleSystem jumpFx;
    public Transform referenceCamera;

    float modeSwitchGraceTimer = 0f;

    public void SuppressFallDamage(float duration = 2.0f)
    {
        modeSwitchGraceTimer = Mathf.Max(modeSwitchGraceTimer, duration);
        if (health != null) health.SuppressDamage(duration);
    }

    Rigidbody rb;
    Renderer rend;
    Material cachedMat;
    Collider col;
    TrailRenderer trail;
    PlayerHealth health;
    Vector2 heldInput;
    Vector2 stickyInput;
    Vector2 keyInput;
    float jumpPressedTime = -100f;
    float lastGroundedTime = -100f;
    bool frozen;
    bool hurtHop;
    bool wasGrounded = true;
    float spawnAudioMuteUntil;
    Vector3 knockVel;
    float knockTimer;
    Vector3 localRespawnPoint;
    Color startColor;
    float baseTrailWidthMultiplier = 1f;
    float baseTrailMinVertexDist = 0.1f;
    float lastSyncedScale = 1f;

    // Moving platform momentum inheritance
    Vector3 activePlatformVelocity;
    Vector3 lastPlatformVelocity;
    Vector3 airbornePlatformMomentum;

    // Parent transform tracking so scaling, rotating, or moving the AR map moves the cube cleanly with it
    Transform trackedParent;
    Matrix4x4 lastParentLocalToWorld = Matrix4x4.identity;
    Matrix4x4 lastParentWorldToLocal = Matrix4x4.identity;
    bool hasParentSnapshot;

    // Cardinal corridor alignment + hold direction hysteresis so held directions don't drift into walls
    Vector2 lastSingleRawInput;
    Vector2 lockedLocalCardinal;
    bool hasLockedLocalCardinal;

    static readonly Collider[] hits = new Collider[8];
    readonly HashSet<UIHoldButton> activeHoldButtons = new HashSet<UIHoldButton>();
    static readonly List<UIHoldButton> tempButtons = new List<UIHoldButton>();

    public static event System.Action<bool> OnStickyModeChanged;

    public static bool PreferredStickyMode
    {
        get
        {
            if (PlayerPrefs.HasKey(MoveModePrefKey))
                return PlayerPrefs.GetInt(MoveModePrefKey, 1) == 1;
            var cc = FindFirstObjectByType<CubeController>(FindObjectsInactive.Include);
            return cc == null || cc.stickyMode;
        }
        set
        {
            PlayerPrefs.SetInt(MoveModePrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
            var cc = FindFirstObjectByType<CubeController>(FindObjectsInactive.Include);
            if (cc != null)
            {
                cc.stickyMode = value;
                cc.StopMovement();
            }
            OnStickyModeChanged?.Invoke(value);
        }
    }

    public Vector3 RespawnPoint
    {
        get { return transform.parent != null ? transform.parent.TransformPoint(localRespawnPoint) : localRespawnPoint; }
    }
    public event System.Action<Color> OnColorChanged;
    public Color CurrentColor { get { return cachedMat != null ? cachedMat.color : (rend != null ? rend.sharedMaterial.color : startColor); } }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        if (rend != null) cachedMat = rend.material;
        col = GetComponent<Collider>();
        trail = GetComponent<TrailRenderer>();
        health = GetComponent<PlayerHealth>();
        rb.freezeRotation = true;
        rb.useGravity = false;
        stickyMode = PreferredStickyMode;
        if (trail != null)
        {
            baseTrailWidthMultiplier = trail.widthMultiplier > 0f ? trail.widthMultiplier : 1f;
            baseTrailMinVertexDist = trail.minVertexDistance > 0f ? trail.minVertexDistance : 0.1f;
        }
        if (jumpFx != null)
        {
            var main = jumpFx.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
        lastSyncedScale = Mathf.Max(0.001f, transform.lossyScale.x);
        SyncScaleDependentEffects(lastSyncedScale, forceClearTrail: false);
        spawnAudioMuteUntil = Time.unscaledTime + 0.35f;
        SetRespawn(transform.position);
        CaptureParentSnapshot();
        if (cachedMat != null)
        {
            startColor = cachedMat.color;
            // Prevent dire invisible black cube: if too dark, default to vibrant radiant cyan
            if (startColor.r < 0.2f && startColor.g < 0.2f && startColor.b < 0.2f)
            {
                startColor = new Color(0.1f, 0.85f, 1f, 1f);
                cachedMat.color = startColor;
            }
        }
        else
        {
            startColor = new Color(0.1f, 0.85f, 1f, 1f);
        }
        OnColorChanged?.Invoke(startColor);
    }

    void OnEnable()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.position = transform.position;
            if (transform.parent != null)
            {
                rb.rotation = transform.parent.rotation;
                transform.rotation = transform.parent.rotation;
            }
        }
        stickyMode = PreferredStickyMode;
        hasLockedLocalCardinal = false;
        ClearAllHeldInputs();
        CaptureParentSnapshot();
    }

    void OnDisable()
    {
        ClearAllHeldInputs();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ClearAllHeldInputs();
    }

    // Hold mode: idempotently track which D-pad buttons are actively pressed.
    public void SetButtonHeld(UIHoldButton btn, bool isHeld)
    {
        if (btn == null) return;
        if (isHeld) activeHoldButtons.Add(btn);
        else activeHoldButtons.Remove(btn);
        RecomputeHeldInput();
    }

    void RecomputeHeldInput()
    {
        Vector2 sum = Vector2.zero;
        foreach (var b in activeHoldButtons)
        {
            if (b != null && b.isActiveAndEnabled) sum += b.direction;
        }
        heldInput = Vector2.ClampMagnitude(sum, 1f);
    }

    public void ClearAllHeldInputs()
    {
        if (activeHoldButtons.Count > 0)
        {
            tempButtons.Clear();
            foreach (var b in activeHoldButtons) tempButtons.Add(b);
            activeHoldButtons.Clear();
            for (int i = 0; i < tempButtons.Count; i++)
            {
                if (tempButtons[i] != null) tempButtons[i].ClearHeldState();
            }
            tempButtons.Clear();
        }
        heldInput = Vector2.zero;
    }

    // Tap mode: tapping a direction latches it, tapping it again stops that axis.
    public void ToggleSticky(Vector2 d)
    {
        if (Mathf.Abs(d.x) > 0.01f) stickyInput.x = Mathf.Approximately(stickyInput.x, d.x) ? 0f : d.x;
        if (Mathf.Abs(d.y) > 0.01f) stickyInput.y = Mathf.Approximately(stickyInput.y, d.y) ? 0f : d.y;
    }

    public void StopMovement()
    {
        stickyInput = Vector2.zero;
        ClearAllHeldInputs();
        airbornePlatformMomentum = Vector3.zero;
        if (rb != null)
        {
            var v = rb.linearVelocity;
            v.x = 0f;
            v.z = 0f;
            rb.linearVelocity = v;
        }
    }

    public void RequestJump()
    {
        if (Time.timeScale == 0f) return;   // paused
        jumpPressedTime = Time.time;
    }

    public void SetFrozen(bool f) { frozen = f; if (f) { stickyInput = Vector2.zero; ClearAllHeldInputs(); airbornePlatformMomentum = Vector3.zero; } }
    public void SetRespawn(Vector3 p)
    {
        localRespawnPoint = transform.parent != null ? transform.parent.InverseTransformPoint(p) : p;
    }

    // Pushes the cube away from something (lava) for a moment and cancels its latched movement.
    public void Knockback(Vector3 dir, float speed, float time)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.back;
        knockVel = dir.normalized * speed;
        knockTimer = time;
        stickyInput = Vector2.zero;
    }

    public void ResetVisual()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (cachedMat == null && rend != null && Application.isPlaying) cachedMat = rend.material;
        if (startColor.r < 0.2f && startColor.g < 0.2f && startColor.b < 0.2f)
            startColor = new Color(0.1f, 0.85f, 1f, 1f);

        if (cachedMat != null)
        {
            cachedMat.color = startColor;
        }
        else if (rend != null && rend.sharedMaterial != null)
        {
            rend.sharedMaterial.color = startColor;
        }
        SetTrailColor(startColor);
        OnColorChanged?.Invoke(startColor);
    }

    public void Hurt()
    {
        hurtHop = true;
        var cam = Camera.main;
        if (cam != null)
        {
            var rig = cam.GetComponent<ThirdPersonCamera>();
            if (rig != null) rig.Shake(0.3f, 0.4f);
        }
        Burst(new Color(1f, 0.25f, 0.2f), 18);
    }

    public void Burst(Color c, int count)
    {
        if (jumpFx == null) return;
        float s = Mathf.Max(0.001f, transform.lossyScale.y);
        jumpFx.transform.position = transform.position + Vector3.down * (0.4f * s);
        var e = new ParticleSystem.EmitParams();
        e.startColor = c;
        jumpFx.Emit(e, count);
    }

    public void SyncScaleDependentEffects(float s, bool forceClearTrail = false)
    {
        s = Mathf.Max(0.001f, s);
        bool scaleJumped = Mathf.Abs(s - lastSyncedScale) > Mathf.Max(0.02f, lastSyncedScale * 0.25f);
        if (trail != null)
        {
            trail.widthMultiplier = baseTrailWidthMultiplier * s;
            trail.minVertexDistance = Mathf.Max(0.001f, baseTrailMinVertexDist * s);
            if (forceClearTrail || scaleJumped)
            {
                trail.Clear();
            }
        }
        if (col != null)
        {
            bool isAR = (DualModeManager.Instance != null && DualModeManager.Instance.currentMode == DualModeManager.GameMode.ARTabletop) || s < 0.5f;
            col.contactOffset = isAR ? Mathf.Clamp(0.01f * s, 0.001f, 0.01f) : 0.01f;
        }
        lastSyncedScale = s;
    }

    void SetTrailColor(Color c)
    {
        if (trail == null) return;
        trail.startColor = new Color(c.r, c.g, c.b, 0.8f);
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
    }

    public Vector2 ActiveMoveInput
    {
        get { return frozen ? Vector2.zero : Vector2.ClampMagnitude(heldInput + stickyInput + keyInput, 1f); }
    }

    public void NotifyParentTransformSynced()
    {
        CaptureParentSnapshot();
    }

    void CaptureParentSnapshot()
    {
        trackedParent = transform.parent;
        if (trackedParent != null)
        {
            lastParentLocalToWorld = trackedParent.localToWorldMatrix;
            lastParentWorldToLocal = trackedParent.worldToLocalMatrix;
            hasParentSnapshot = true;
        }
        else
        {
            hasParentSnapshot = false;
        }
    }

    static bool MatricesApproxEqual(in Matrix4x4 a, in Matrix4x4 b)
    {
        for (int i = 0; i < 16; i++)
        {
            if (Mathf.Abs(a[i] - b[i]) > 1e-5f) return false;
        }
        return true;
    }

    void SyncWithParentTransformIfMoved()
    {
        if (transform.parent == null)
        {
            hasParentSnapshot = false;
            trackedParent = null;
            return;
        }

        if (!hasParentSnapshot || trackedParent != transform.parent)
        {
            CaptureParentSnapshot();
            return;
        }

        Matrix4x4 curLocalToWorld = transform.parent.localToWorldMatrix;
        Quaternion targetRot = transform.parent.rotation;

        if (!MatricesApproxEqual(lastParentLocalToWorld, curLocalToWorld))
        {
            Vector3 prevLocalPos = lastParentWorldToLocal.MultiplyPoint3x4(rb.position);
            Vector3 prevLocalVel = lastParentWorldToLocal.MultiplyVector(rb.linearVelocity);

            Vector3 newWorldPos = curLocalToWorld.MultiplyPoint3x4(prevLocalPos);
            Vector3 newWorldVel = curLocalToWorld.MultiplyVector(prevLocalVel);

            var mode = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.position = newWorldPos;
            transform.position = newWorldPos;
            rb.rotation = targetRot;
            transform.rotation = targetRot;
            rb.linearVelocity = newWorldVel;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = mode;
            Physics.SyncTransforms();

            CaptureParentSnapshot();
        }
        else if (Quaternion.Angle(rb.rotation, targetRot) > 0.05f)
        {
            rb.rotation = targetRot;
            transform.rotation = targetRot;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void TeleportTo(Vector3 p)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        Quaternion targetRot = (transform.parent != null) ? transform.parent.rotation : Quaternion.identity;
        if (rb != null)
        {
            var mode = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = p;
            rb.rotation = targetRot;
            transform.position = p;
            transform.rotation = targetRot;
            rb.interpolation = mode;
        }
        else
        {
            transform.position = p;
            transform.rotation = targetRot;
        }
        stickyInput = Vector2.zero;
        hasLockedLocalCardinal = false;
        ClearAllHeldInputs();
        jumpPressedTime = -100f;
        lastGroundedTime = -100f;
        airbornePlatformMomentum = Vector3.zero;
        activePlatformVelocity = Vector3.zero;
        lastPlatformVelocity = Vector3.zero;
        hurtHop = false;
        wasGrounded = true;
        spawnAudioMuteUntil = Time.unscaledTime + 0.35f;
        knockTimer = 0f;
        SyncScaleDependentEffects(Mathf.Max(0.001f, transform.lossyScale.x), forceClearTrail: true);
        Physics.SyncTransforms();
        CaptureParentSnapshot();
    }

    void LateUpdate()
    {
        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        if (Mathf.Abs(s - lastSyncedScale) > 0.0001f)
            SyncScaleDependentEffects(s, forceClearTrail: false);
    }

    void Update()
    {
        keyInput = Vector2.zero;
        if (Time.timeScale == 0f) return;   // paused
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) keyInput.x -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) keyInput.x += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) keyInput.y -= 1f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) keyInput.y += 1f;
            if (k.spaceKey.wasPressedThisFrame) RequestJump();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyInput.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyInput.x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyInput.y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyInput.y += 1f;
        if (Input.GetKeyDown(KeyCode.Space)) RequestJump();
#endif
    }

    // A wide box just under the feet, so standing on an edge still counts as grounded.
    bool CheckGrounded()
    {
        float s = Mathf.Max(0.001f, transform.lossyScale.y);
        Vector3 center = rb.position + Vector3.down * (0.55f * s);
        Vector3 half = new Vector3(0.42f * s, 0.08f * s, 0.42f * s);
        int n = Physics.OverlapBoxNonAlloc(center, half, hits, rb.rotation, ~0, QueryTriggerInteraction.Ignore);
        bool isGrounded = false;
        activePlatformVelocity = Vector3.zero;

        for (int i = 0; i < n; i++)
        {
            if (hits[i] != col)
            {
                isGrounded = true;
                var mp = hits[i].GetComponentInParent<MovingPlatform>();
                if (mp != null)
                {
                    activePlatformVelocity = mp.CurrentVelocity;
                    break;
                }
                var jp = hits[i].GetComponentInParent<JumpingPlatform>();
                if (jp != null)
                {
                    activePlatformVelocity = jp.CurrentVelocity;
                    break;
                }
            }
        }
        return isGrounded;
    }

    Vector3 ComputeMoveDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.0001f)
        {
            hasLockedLocalCardinal = false;
            return Vector3.zero;
        }

        Vector3 mazeRight = transform.parent != null ? transform.parent.right : Vector3.right;
        mazeRight.y = 0f;
        mazeRight = (mazeRight.sqrMagnitude > 0.0001f) ? mazeRight.normalized : Vector3.right;

        Vector3 mazeFwd = transform.parent != null ? transform.parent.forward : Vector3.forward;
        mazeFwd.y = 0f;
        mazeFwd = (mazeFwd.sqrMagnitude > 0.0001f) ? mazeFwd.normalized : Vector3.forward;

        if (referenceCamera == null)
        {
            hasLockedLocalCardinal = false;
            return mazeRight * input.x + mazeFwd * input.y;
        }

        // Smooth nadir-safe camera horizontal basis (avoids 90-deg flip when looking steeply down in AR)
        Vector3 fwdFlat = referenceCamera.forward;
        fwdFlat.y = 0f;
        Vector3 upFlat = referenceCamera.up;
        upFlat.y = 0f;
        Vector3 camFwd = fwdFlat + upFlat * 0.35f;
        if (camFwd.sqrMagnitude < 0.0001f) camFwd = mazeFwd;
        camFwd.y = 0f;
        camFwd.Normalize();

        Vector3 camRight = Vector3.Cross(Vector3.up, camFwd);
        if (camRight.sqrMagnitude < 0.0001f) camRight = mazeRight;
        else camRight.Normalize();

        Vector3 worldWish = camRight * input.x + camFwd * input.y;
        if (worldWish.sqrMagnitude < 0.0001f)
        {
            hasLockedLocalCardinal = false;
            return Vector3.zero;
        }

        // Single-direction input snaps cleanly onto the closest maze corridor axis with hold hysteresis
        bool isSingleCardinal = (Mathf.Abs(input.x) > 0.1f) ^ (Mathf.Abs(input.y) > 0.1f);
        if (isSingleCardinal)
        {
            Vector3 wishNorm = worldWish.normalized;
            if (hasLockedLocalCardinal && (input - lastSingleRawInput).sqrMagnitude < 0.04f)
            {
                Vector3 currentLockedWorld = mazeRight * lockedLocalCardinal.x + mazeFwd * lockedLocalCardinal.y;
                if (Vector3.Dot(wishNorm, currentLockedWorld) > 0.45f)
                {
                    return currentLockedWorld;
                }
            }

            float rx = Vector3.Dot(wishNorm, mazeRight);
            float fz = Vector3.Dot(wishNorm, mazeFwd);
            Vector2 chosenLocal = (Mathf.Abs(rx) >= Mathf.Abs(fz))
                ? new Vector2(rx >= 0f ? 1f : -1f, 0f)
                : new Vector2(0f, fz >= 0f ? 1f : -1f);

            lastSingleRawInput = input;
            lockedLocalCardinal = chosenLocal;
            hasLockedLocalCardinal = true;
            return mazeRight * chosenLocal.x + mazeFwd * chosenLocal.y;
        }

        hasLockedLocalCardinal = false;
        return worldWish;
    }

    void FixedUpdate()
    {
        if (rb.useGravity) rb.useGravity = false;
        SyncWithParentTransformIfMoved();

        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        if (Mathf.Abs(s - lastSyncedScale) > 0.0001f)
            SyncScaleDependentEffects(s, forceClearTrail: false);
        bool isARMode = (DualModeManager.Instance != null && DualModeManager.Instance.currentMode == DualModeManager.GameMode.ARTabletop) || s < 0.5f;
        float curMoveSpeed = moveSpeed * s;
        // Scale both jump velocity AND gravity linearly by s so jump arc & flight duration match 3D mode at every AR scale
        float curJumpSpeed = jumpSpeed * s * (isARMode ? arJumpVelocityMultiplier : 1f);

        if (modeSwitchGraceTimer > 0f)
        {
            modeSwitchGraceTimer -= Time.fixedDeltaTime;
        }
        else
        {
            // Fell into a void: back to the last respawn point (and lose a heart where that applies).
            float localPosY = (transform.parent != null) ? transform.parent.InverseTransformPoint(rb.position).y : rb.position.y;
            if (localPosY < fallY)
            {
                GameAudioManager.Instance?.PlayFallVoid();
                HapticsManager.HeavyHit();
                if (fallsCostHealth && health != null) health.ForceDamage(1, customSfx: true);
                TeleportTo(RespawnPoint);
                return;
            }
        }

        Vector3 v = rb.linearVelocity;

        bool grounded = CheckGrounded();
        if (grounded)
        {
            if (v.y <= 0.25f * s) lastGroundedTime = Time.time;
            lastPlatformVelocity = activePlatformVelocity;
            airbornePlatformMomentum = Vector3.zero;
        }
        else if (wasGrounded)
        {
            // Just became airborne (e.g. stepped off platform into empty air)
            airbornePlatformMomentum = lastPlatformVelocity;
        }

        if (!wasGrounded && grounded && Time.unscaledTime > spawnAudioMuteUntil && v.y < -1.2f * s)
        {
            GameAudioManager.Instance?.PlayLand();
            HapticsManager.Land();
        }
        wasGrounded = grounded;

        // Horizontal velocity is set independently of vertical velocity,
        // so you keep full control while jumping and moving at the same time.
        Vector2 input = frozen ? Vector2.zero : Vector2.ClampMagnitude(heldInput + stickyInput + keyInput, 1f);
        Vector3 moveDir = ComputeMoveDirection(input);
        v.x = moveDir.x * curMoveSpeed;
        v.z = moveDir.z * curMoveSpeed;

        if (input.sqrMagnitude > 0.04f && grounded && !frozen && Time.timeScale > 0f)
        {
            GameAudioManager.Instance?.PlayCubeStep();
            HapticsManager.MoveStep();
        }

        if (knockTimer > 0f)
        {
            knockTimer -= Time.fixedDeltaTime;
            v.x = knockVel.x * s;
            v.z = knockVel.z * s;
        }

        // Jump buffer + coyote time: a jump pressed slightly early or slightly late still works.
        bool jumpedThisStep = false;
        if (!frozen
            && Time.time - jumpPressedTime <= jumpBufferTime
            && Time.time - lastGroundedTime <= coyoteTime)
        {
            v.y = curJumpSpeed;
            jumpedThisStep = true;
            jumpPressedTime = -100f;
            lastGroundedTime = -100f;
            wasGrounded = false;
            grounded = false;
            airbornePlatformMomentum = (activePlatformVelocity.sqrMagnitude > 0.001f) ? activePlatformVelocity : lastPlatformVelocity;
            ChangeColor();
            if (cachedMat != null) Burst(cachedMat.color, 10);
            GameAudioManager.Instance?.PlayJump();
            HapticsManager.Jump();
        }

        // Inherit moving platform horizontal momentum while airborne
        if (!grounded)
        {
            v.x += airbornePlatformMomentum.x;
            v.z += airbornePlatformMomentum.z;
        }

        // Small hop when hurt.
        if (hurtHop) { v.y = Mathf.Max(v.y, 5.5f * s); hurtHop = false; }

        // Scale-invariant custom gravity (both rising and falling scale linearly by s)
        if (!jumpedThisStep)
        {
            float activeFallMult = isARMode ? arFallGravityMultiplier : fallGravityMultiplier;
            float gravScale = (v.y > 0f) ? 1f : activeFallMult;
            v.y += Physics.gravity.y * s * gravScale * Time.fixedDeltaTime;
        }

        rb.linearVelocity = v;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Time.unscaledTime <= spawnAudioMuteUntil || frozen) return;
        if (collision.collider.GetComponent<LavaWall>() != null) return;

        float s = Mathf.Max(0.001f, transform.lossyScale.x);
        if (collision.relativeVelocity.magnitude < 1.25f * s) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            // Side wall/obstacle hit rather than vertical floor support
            if (Mathf.Abs(contact.normal.y) < 0.55f)
            {
                if (!wasGrounded && airbornePlatformMomentum.sqrMagnitude > 0.001f)
                {
                    airbornePlatformMomentum = Vector3.ProjectOnPlane(airbornePlatformMomentum, contact.normal);
                }
                GameAudioManager.Instance?.PlayWallBump();
                HapticsManager.BumpWall();
                break;
            }
        }
    }

    void ChangeColor()
    {
        if (cachedMat == null)
        {
            if (rend == null) return;
            cachedMat = rend.material;
        }
        Color c = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        cachedMat.color = c;
        SetTrailColor(c);
        OnColorChanged?.Invoke(c);
    }
}
