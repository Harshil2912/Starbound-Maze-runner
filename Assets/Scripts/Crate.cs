using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Crate : MonoBehaviour
{
    [Header("Spawn & Position")]
    [Tooltip("Default local spawn position relative to parent (inside the Level 3 puzzle room)")]
    public Vector3 defaultSpawnLocalPos = new Vector3(14.00f, 0.76f, 2.00f);

    public float fallY = -6f;
    public float floorLocalY = 0.76f; // Floor surface is at local y=0, crate half-height is 0.75f

    [Header("Push Dynamics")]
    public float pushResponsiveness = 4.5f;
    public float decelerationRate = 8.0f;

    Rigidbody rb;
    Collider col;
    Transform trackedParent;
    Matrix4x4 lastParentLocalToWorld = Matrix4x4.identity;
    Matrix4x4 lastParentWorldToLocal = Matrix4x4.identity;
    bool hasParentSnapshot;

    Vector3 pushVelocity;
    float pushTimer;
    static readonly Collider[] overlapBuf = new Collider[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        ConfigureRigidbody();

        // Ensure defaultSpawnLocalPos is never zero
        if (defaultSpawnLocalPos == Vector3.zero)
        {
            if (transform.localPosition != Vector3.zero)
                defaultSpawnLocalPos = transform.localPosition;
            else
                defaultSpawnLocalPos = new Vector3(14.00f, 0.76f, 2.00f);
        }

        CaptureParentSnapshot();
    }

    void OnEnable()
    {
        ResetCrate();
    }

    void ConfigureRigidbody()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.mass = 2.2f; // Satisfies requiredMass = 2.0f for Heavy Plate
        rb.linearDamping = 3f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void CaptureParentSnapshot()
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
            if (Mathf.Abs(a[i] - b[i]) > 1e-4f) return false;
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
            Vector3 prevLocalPos = lastParentWorldToLocal.MultiplyPoint3x4(rb != null ? rb.position : transform.position);
            Vector3 newWorldPos = curLocalToWorld.MultiplyPoint3x4(prevLocalPos);

            if (rb != null)
            {
                var mode = rb.interpolation;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.position = newWorldPos;
                transform.position = newWorldPos;
                rb.rotation = targetRot;
                transform.rotation = targetRot;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.interpolation = mode;
            }
            else
            {
                transform.position = newWorldPos;
                transform.rotation = targetRot;
            }

            Physics.SyncTransforms();
            CaptureParentSnapshot();
        }
        else if (rb != null && Quaternion.Angle(rb.rotation, targetRot) > 0.05f)
        {
            rb.rotation = targetRot;
            transform.rotation = targetRot;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ResetCrate()
    {
        ConfigureRigidbody();
        if (defaultSpawnLocalPos == Vector3.zero)
            defaultSpawnLocalPos = new Vector3(14.00f, 0.76f, 2.00f);

        transform.localPosition = defaultSpawnLocalPos;
        Quaternion targetRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;

        if (rb != null)
        {
            var mode = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.position = transform.position;
            rb.rotation = targetRot;
            transform.rotation = targetRot;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = mode;
        }
        else
        {
            transform.rotation = targetRot;
        }

        pushVelocity = Vector3.zero;
        pushTimer = 0f;

        float s = transform.parent != null ? Mathf.Max(0.001f, transform.parent.lossyScale.x) : Mathf.Max(0.001f, transform.lossyScale.x);
        if (col == null) col = GetComponent<Collider>();
        if (col != null)
        {
            bool isAR = (DualModeManager.Instance != null && DualModeManager.Instance.currentMode == DualModeManager.GameMode.ARTabletop) || s < 0.5f;
            col.contactOffset = isAR ? Mathf.Clamp(0.01f * s, 0.001f, 0.01f) : 0.01f;
        }

        Physics.SyncTransforms();
        CaptureParentSnapshot();
    }

    public bool IsOnPlate()
    {
        float s = transform.parent != null ? Mathf.Max(0.001f, transform.parent.lossyScale.x) : Mathf.Max(0.001f, transform.lossyScale.x);
        int count = Physics.OverlapBoxNonAlloc(transform.position, Vector3.one * (0.8f * s), overlapBuf, transform.rotation, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            if (overlapBuf[i] != null && overlapBuf[i].GetComponentInParent<PressurePlate>() != null) return true;
        }
        return false;
    }

    void OnCollisionStay(Collision collision)
    {
        // Detect player cube pushing against the crate
        var player = collision.collider.GetComponentInParent<CubeController>();
        if (player == null) return;

        // Find horizontal push direction from player to crate in parent local space
        Vector3 playerPos = player.transform.position;
        Vector3 toCrate = transform.position - playerPos;

        Vector3 parentUp = transform.parent != null ? transform.parent.up : Vector3.up;
        Vector3 flatPush = Vector3.ProjectOnPlane(toCrate, parentUp);
        if (flatPush.sqrMagnitude > 0.0001f)
        {
            float s = transform.parent != null ? Mathf.Max(0.001f, transform.parent.lossyScale.x) : 1f;
            Vector3 pushDir = flatPush.normalized;
            pushVelocity = pushDir * (pushResponsiveness * s);
            pushTimer = 0.1f; // maintain push momentum for a short window
        }
    }

    void FixedUpdate()
    {
        SyncWithParentTransformIfMoved();

        if (rb == null) return;
        if (rb.useGravity) rb.useGravity = false;

        float s = transform.parent != null ? Mathf.Max(0.001f, transform.parent.lossyScale.x) : 1f;

        // Check if crate fell into void
        float localPosY = (transform.parent != null) ? transform.parent.InverseTransformPoint(rb.position).y : rb.position.y;
        if (localPosY < fallY)
        {
            ResetCrate();
            return;
        }

        // Clamp crate vertical position so it rests perfectly on the floor and never sinks into the ground
        if (localPosY < floorLocalY - 0.05f)
        {
            if (transform.parent != null)
            {
                Vector3 localP = transform.parent.InverseTransformPoint(rb.position);
                localP.y = floorLocalY;
                Vector3 worldP = transform.parent.TransformPoint(localP);
                rb.position = worldP;
                transform.position = worldP;
            }
            else
            {
                Vector3 p = rb.position;
                p.y = floorLocalY;
                rb.position = p;
                transform.position = p;
            }
        }

        Vector3 curVel = rb.linearVelocity;

        if (pushTimer > 0f)
        {
            pushTimer -= Time.fixedDeltaTime;
            // Smoothly apply push velocity
            curVel.x = pushVelocity.x;
            curVel.z = pushVelocity.z;
        }
        else
        {
            // Decelerate horizontal movement when not being actively pushed
            curVel.x = Mathf.MoveTowards(curVel.x, 0f, decelerationRate * s * Time.fixedDeltaTime);
            curVel.z = Mathf.MoveTowards(curVel.z, 0f, decelerationRate * s * Time.fixedDeltaTime);
        }

        // Apply scale-invariant downward gravity relative to parent orientation
        Vector3 parentDown = transform.parent != null ? -transform.parent.up : Vector3.down;
        if (localPosY > floorLocalY + 0.02f)
        {
            curVel += parentDown * (9.81f * s * Time.fixedDeltaTime);
        }
        else
        {
            // Grounded on floor: zero out downward velocity
            if (Vector3.Dot(curVel, parentDown) > 0f)
            {
                curVel = Vector3.ProjectOnPlane(curVel, parentDown);
            }
        }

        rb.linearVelocity = curVel;
    }
}
