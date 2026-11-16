using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

public class DualModeManager : MonoBehaviour
{
    static DualModeManager _instance;
    public static DualModeManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindAnyObjectByType<DualModeManager>();
            return _instance;
        }
    }

    public enum GameMode { Normal3D, ARTabletop }

    [Header("Current Mode")]
    public GameMode currentMode = GameMode.Normal3D;

    [Header("Camera & Rig References")]
    public GameObject normalCameraRig; // Main Camera with ThirdPersonCamera
    public GameObject arSession;      // AR Session
    public GameObject xrOriginRig;    // XR Origin / AR Session Origin
    public Transform arContentRoot;   // AR Content Anchor
    public Transform activeLevelAnchor; // Level Pivot under ARContentRoot
    public GameObject shadowCatcherPlane; // AR Shadow Catcher Plane (visible ONLY when AR placed)

    [Header("Core References")]
    public CubeController player;
    public LevelManager levelManager;
    public ARPlacementManager arPlacementManager;
    public ARHologramManager arHologramManager;

    [Header("UI Controls")]
    public Button modeSwitchButton;
    public Text modeSwitchText;
    public Button hudModeSwitchButton;
    public Text hudModeSwitchText;
    public Button repositionButton;

    const string ModePrefKey = "MR_SelectedGameMode";
    bool isRequestingPermission;
    bool arSessionStartedOnce;

    public static event System.Action<GameMode> OnModeChanged;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // Force Landscape orientation at runtime across all devices & optimize mobile frame pacing
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (player == null) player = FindAnyObjectByType<CubeController>();
        if (levelManager == null) levelManager = FindAnyObjectByType<LevelManager>();
        if (arHologramManager == null) arHologramManager = FindAnyObjectByType<ARHologramManager>();
        if (shadowCatcherPlane == null && arContentRoot != null)
        {
            var t = arContentRoot.Find("AR_ShadowCatcherPlane");
            if (t != null) shadowCatcherPlane = t.gameObject;
        }
    }

    void Start()
    {
        if (modeSwitchButton != null)
        {
            modeSwitchButton.onClick.AddListener(ToggleMode);
        }
        if (hudModeSwitchButton != null)
        {
            hudModeSwitchButton.onClick.AddListener(ToggleMode);
        }
        if (repositionButton != null)
        {
            repositionButton.onClick.AddListener(OnRepositionClicked);
        }

        int savedMode = PlayerPrefs.GetInt(ModePrefKey, (int)GameMode.Normal3D);
        currentMode = (GameMode)savedMode;
        PrepareForMenu();
        UpdateUI();
    }

    public void ToggleMode()
    {
        if (isRequestingPermission) return;
        HapticsManager.SuppressFor(0.35f);
        GameAudioManager.Instance?.StopAllSfx();
        GameAudioManager.Instance?.PlayUIClick();
        if (player != null) player.StopMovement();

        GameMode next = (currentMode == GameMode.Normal3D) ? GameMode.ARTabletop : GameMode.Normal3D;

        var ui = FindAnyObjectByType<GameUI>();
        bool isMenuOpen = (ui == null || ui.IsMenuOpen);

        if (isMenuOpen)
        {
            currentMode = next;
            PlayerPrefs.SetInt(ModePrefKey, (int)currentMode);
            PlayerPrefs.Save();
            UpdateUI();
            OnModeChanged?.Invoke(currentMode);
        }
        else
        {
            SetMode(next, true);
        }
    }

    public void PrepareForMenu()
    {
        StopAllCoroutines();
        isRequestingPermission = false;
        HapticsManager.SuppressFor(0.35f);
        GameAudioManager.Instance?.StopAllSfx();
        if (player != null) player.StopMovement();
        SetupNormal3DMode(snapCamera: false);
        UpdateUI();
    }

    public void ApplyCurrentModeToGameplay()
    {
        SetMode(currentMode, false);
    }

    public void SetMode(GameMode mode, bool savePref = true)
    {
        StopAllCoroutines();
        isRequestingPermission = false;
        HapticsManager.SuppressFor(0.35f);
        GameAudioManager.Instance?.StopAllSfx();
        if (player != null)
        {
            player.StopMovement();
            player.SuppressFallDamage(2.5f);
        }

        currentMode = mode;
        if (savePref)
        {
            PlayerPrefs.SetInt(ModePrefKey, (int)mode);
            PlayerPrefs.Save();
        }

        if (mode == GameMode.Normal3D)
        {
            var ui = FindAnyObjectByType<GameUI>();
            bool isGameplay = ui != null && !ui.IsMenuOpen;
            SetupNormal3DMode(snapCamera: isGameplay);
        }
        else
        {
            // Hide diorama immediately so physics/hazards cannot tick before tabletop placement
            if (arContentRoot != null) arContentRoot.gameObject.SetActive(false);
            if (shadowCatcherPlane != null) shadowCatcherPlane.SetActive(false);
            StartCoroutine(SetupARTabletopModeRoutine());
        }

        UpdateUI();
        OnModeChanged?.Invoke(currentMode);
    }

    void EnsureTrackedPoseDriverConfigured(Camera arCam)
    {
#if ENABLE_INPUT_SYSTEM
        if (arCam == null) return;
        var tpd = arCam.GetComponent<TrackedPoseDriver>();
        if (tpd == null) tpd = arCam.gameObject.AddComponent<TrackedPoseDriver>();

        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

        bool needsPos = (tpd.positionInput.action == null || tpd.positionInput.action.bindings.Count == 0);
        bool needsRot = (tpd.rotationInput.action == null || tpd.rotationInput.action.bindings.Count == 0);

        if (needsPos)
        {
            var posAction = new InputAction("Position", InputActionType.Value, expectedControlType: "Vector3");
            posAction.AddBinding("<HandheldARInputDevice>/devicePosition");
            posAction.AddBinding("<XRHMD>/centerEyePosition");
            tpd.positionInput = new InputActionProperty(posAction);
        }
        if (needsRot)
        {
            var rotAction = new InputAction("Rotation", InputActionType.Value, expectedControlType: "Quaternion");
            rotAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
            rotAction.AddBinding("<XRHMD>/centerEyeRotation");
            tpd.rotationInput = new InputActionProperty(rotAction);
        }
#endif
    }

    void SetupNormal3DMode(bool snapCamera)
    {
        // 1. Wipe previously detected AR planes & reset active ARSession cleanly so re-entering AR rescans fresh
        if (arPlacementManager != null)
        {
            arPlacementManager.CancelPlacementUI();
            arPlacementManager.ClearAllPlanes();
        }

        if (xrOriginRig != null)
        {
            var planeMgr = xrOriginRig.GetComponentInChildren<ARPlaneManager>(true);
            if (planeMgr != null) planeMgr.enabled = false;
            var rayMgr = xrOriginRig.GetComponentInChildren<ARRaycastManager>(true);
            if (rayMgr != null) rayMgr.enabled = false;

            var arCam = xrOriginRig.GetComponentInChildren<Camera>(true);
            if (arCam != null)
            {
                arCam.enabled = false;
                if (arCam.CompareTag("MainCamera")) arCam.tag = "Untagged";
                var arListener = arCam.GetComponent<AudioListener>();
                if (arListener != null) arListener.enabled = false;
            }
            xrOriginRig.SetActive(false);
        }

        if (arSession != null)
        {
            var session = arSession.GetComponent<ARSession>();
            if (session != null)
            {
                if (arSessionStartedOnce && arSession.activeInHierarchy && session.enabled)
                {
                    session.Reset();
                }
                session.enabled = false;
            }
            arSession.SetActive(false);
        }

        // 2. Activate standard camera rig cleanly
        if (normalCameraRig != null)
        {
            normalCameraRig.SetActive(true);
            normalCameraRig.tag = "MainCamera";
            var cam = normalCameraRig.GetComponent<Camera>();
            if (cam != null) cam.enabled = true;
            var listener = normalCameraRig.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        // 3. Normal controls (world-aligned)
        if (player != null)
        {
            player.SuppressFallDamage(2.5f);
            player.referenceCamera = null;
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = player.transform.position;
            }
        }

        // 4. Normal materials & textures (turn off hologram, hide AR shadow catcher)
        if (arHologramManager != null)
        {
            arHologramManager.SetHologramMode(false);
        }
        if (shadowCatcherPlane != null)
        {
            shadowCatcherPlane.SetActive(false);
        }

        // 5. Content scale and position reset for standard 3D play
        if (arContentRoot != null)
        {
            arContentRoot.localScale = Vector3.one;
            arContentRoot.position = Vector3.zero;
            arContentRoot.rotation = Quaternion.identity;
            arContentRoot.gameObject.SetActive(true);
        }

        if (activeLevelAnchor != null)
        {
            activeLevelAnchor.localPosition = Vector3.zero;
        }

        SyncPhysicsContactOffset(1f);
        SetWallCollidersARMode(false);
        ResetAllCrates();
        if (player != null) player.SyncScaleDependentEffects(1f, forceClearTrail: true);
        Physics.SyncTransforms();

        if (snapCamera && levelManager != null && levelManager.cameraRig != null)
        {
            levelManager.cameraRig.Snap();
        }

        if (repositionButton != null) repositionButton.gameObject.SetActive(false);
    }

    public void OnLevelEntered(int levelNumber)
    {
        if (currentMode == GameMode.ARTabletop && activeLevelAnchor != null)
        {
            Vector3 offset = Vector3.zero;
            if (levelNumber == 2) offset = new Vector3(0f, 0f, 100f);
            else if (levelNumber == 3) offset = new Vector3(0f, 0f, 200f);
            else if (levelNumber == 4) offset = new Vector3(0f, 0f, 400f);
            else if (levelNumber == 5) offset = new Vector3(0f, 0f, 600f);

            activeLevelAnchor.localPosition = -offset;
        }
        else if (activeLevelAnchor != null)
        {
            activeLevelAnchor.localPosition = Vector3.zero;
        }

        SetWallCollidersARMode(currentMode == GameMode.ARTabletop);
        ResetAllCrates();

        if (currentMode == GameMode.ARTabletop && arPlacementManager != null && arPlacementManager.isPlaced && arHologramManager != null)
        {
            arHologramManager.SetHologramMode(true);
        }
        else if (arHologramManager != null && currentMode == GameMode.Normal3D)
        {
            arHologramManager.SetHologramMode(false);
        }
    }

    IEnumerator SetupARTabletopModeRoutine()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            isRequestingPermission = true;
            bool handled = false;
            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += (_) => { handled = true; };
            callbacks.PermissionDenied += (_) => { handled = true; };

            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks);

            float timeout = 15f;
            while (!handled && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            isRequestingPermission = false;

            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                Debug.LogWarning("[DualModeManager] Camera permission denied by user. Reverting to 3D Normal mode.");
                SetMode(GameMode.Normal3D, true);
                yield break;
            }
        }
#endif

        yield return null;

        // Ensure Base/level materials start clean until user physically places the AR diorama
        if (arHologramManager != null) arHologramManager.SetHologramMode(false);
        if (shadowCatcherPlane != null) shadowCatcherPlane.SetActive(false);

        // 1. Deactivate standard camera rig cleanly first
        if (normalCameraRig != null)
        {
            var listener = normalCameraRig.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            var cam = normalCameraRig.GetComponent<Camera>();
            if (cam != null) cam.enabled = false;
            if (normalCameraRig.CompareTag("MainCamera")) normalCameraRig.tag = "Untagged";
            normalCameraRig.SetActive(false);
        }

        // 2. Ensure XR Management loader is initialized & running before starting ARSession/ARCamera
        var xrGeneral = XRGeneralSettings.Instance;
        if (xrGeneral != null && xrGeneral.Manager != null)
        {
            if (xrGeneral.Manager.activeLoader == null)
            {
                yield return xrGeneral.Manager.InitializeLoader();
            }
            if (xrGeneral.Manager.activeLoader != null)
            {
                xrGeneral.Manager.StartSubsystems();
            }
            else
            {
                Debug.LogWarning("[DualModeManager] XR activeLoader failed to initialize. Device may lack ARCore support.");
            }
        }

        // 3. Activate AR Session FIRST and let async availability/startup complete BEFORE activating XR Origin
        ARSession session = null;
        if (arSession != null)
        {
            arSession.SetActive(true);
            session = arSession.GetComponent<ARSession>();
            if (session != null)
            {
                session.attemptUpdate = true;
                session.enabled = true;
            }

            float wait = 5f;
            while (wait > 0f &&
                   ARSession.state != ARSessionState.Ready &&
                   ARSession.state != ARSessionState.SessionInitializing &&
                   ARSession.state != ARSessionState.SessionTracking &&
                   ARSession.state != ARSessionState.Unsupported &&
                   ARSession.state != ARSessionState.NeedsInstall)
            {
                wait -= Time.unscaledDeltaTime;
                yield return null;
            }

            // Force a clean ARCore session reset on subsequent AR entries so even identical surfaces rescan fresh
            if (arSessionStartedOnce && session != null && session.enabled)
            {
                if (arPlacementManager != null) arPlacementManager.ClearAllPlanes();
                session.Reset();
                yield return null;
            }
            arSessionStartedOnce = true;
        }

        // 4. Activate XR Origin & AR Camera components
        Camera arCam = null;
        if (xrOriginRig != null)
        {
            // Temporarily ensure plane manager starts fresh after clearing leftover planes
            var planeMgr = xrOriginRig.GetComponentInChildren<ARPlaneManager>(true);
            if (planeMgr != null) planeMgr.enabled = false;

            if (arPlacementManager != null) arPlacementManager.ClearAllPlanes();

            xrOriginRig.SetActive(true);
            arCam = xrOriginRig.GetComponentInChildren<Camera>(true);
            if (arCam != null)
            {
                arCam.tag = "MainCamera";
                arCam.clearFlags = CameraClearFlags.SolidColor;
                arCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                arCam.enabled = true;

                EnsureTrackedPoseDriverConfigured(arCam);

                var arListener = arCam.GetComponent<AudioListener>();
                if (arListener != null) arListener.enabled = true;

                var camMgr = arCam.GetComponent<ARCameraManager>();
                if (camMgr != null) camMgr.enabled = true;

                var camBg = arCam.GetComponent<ARCameraBackground>();
                if (camBg != null) camBg.enabled = true;
            }

            if (planeMgr != null)
            {
                planeMgr.requestedDetectionMode = PlaneDetectionMode.Horizontal;
                planeMgr.enabled = true;
            }

            var rayMgr = xrOriginRig.GetComponentInChildren<ARRaycastManager>(true);
            if (rayMgr != null) rayMgr.enabled = true;
        }

        // 5. Camera-relative movement & placement links
        if (arPlacementManager != null && arCam != null)
        {
            arPlacementManager.arCamera = arCam;
        }
        if (player != null)
        {
            Transform refCam = (arCam != null) ? arCam.transform : (arPlacementManager != null && arPlacementManager.arCamera != null ? arPlacementManager.arCamera.transform : null);
            player.referenceCamera = refCam;
        }

        // 6. Center the active level on the tabletop anchor and pre-sync tabletop physics tolerances
        float arScale = (arPlacementManager != null) ? arPlacementManager.defaultScale : 0.020f;
        SyncPhysicsContactOffset(arScale);
        OnLevelEntered(levelManager != null ? levelManager.CurrentLevel : 1);

        // 7. Trigger placement scanning (diorama remains hidden until table/floor is tracked & tapped)
        if (arPlacementManager != null)
        {
            arPlacementManager.Reposition();
        }

        if (repositionButton != null)
        {
            var ui = FindAnyObjectByType<GameUI>();
            bool isMenuOpen = ui != null && ui.IsMenuOpen;
            repositionButton.gameObject.SetActive(!isMenuOpen);
        }
    }

    void OnRepositionClicked()
    {
        if (ControlsLayoutManager.IsEditingLayout) return;
        GameAudioManager.Instance?.PlayUIClick();
        HapticsManager.LightTap();
        if (arPlacementManager != null)
        {
            arPlacementManager.Reposition();
        }
    }

    void UpdateUI()
    {
        string menuLabel = (currentMode == GameMode.Normal3D) ? "MODE: 3D NORMAL" : "MODE: AR TABLETOP";
        string hudLabel = (currentMode == GameMode.Normal3D) ? "VIEW: 3D NORMAL" : "VIEW: AR TABLETOP";
        if (modeSwitchText != null)
        {
            modeSwitchText.text = menuLabel;
        }
        if (hudModeSwitchText != null)
        {
            hudModeSwitchText.text = hudLabel;
        }
    }

    public void SyncPhysicsContactOffset(float scale)
    {
        bool isAR = (currentMode == GameMode.ARTabletop) || scale < 0.5f;
        float offset = isAR ? Mathf.Clamp(0.01f * scale, 0.001f, 0.01f) : 0.01f;
        Physics.defaultContactOffset = offset;
        if (player != null)
        {
            var pcCol = player.GetComponent<Collider>();
            if (pcCol != null) pcCol.contactOffset = offset;
        }
        var crates = FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < crates.Length; i++)
        {
            if (crates[i] != null)
            {
                var col = crates[i].GetComponent<Collider>();
                if (col != null) col.contactOffset = offset;
            }
        }
    }

    public void ResetAllCrates()
    {
        var crates = FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < crates.Length; i++)
        {
            if (crates[i] != null) crates[i].ResetCrate();
        }
    }

    public void SetWallCollidersARMode(bool isAR)
    {
        var cols = (activeLevelAnchor != null)
            ? activeLevelAnchor.GetComponentsInChildren<BoxCollider>(true)
            : FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < cols.Length; i++)
        {
            var col = cols[i];
            if (col == null || col.isTrigger) continue;

            bool isWall = (col.transform.parent != null && col.transform.parent.name.Contains("Walls"))
                       || col.name.StartsWith("Wall_") || col.name.StartsWith("HWall_") || col.name.StartsWith("VWall_")
                       || (col.transform.parent != null && col.transform.parent.parent != null && col.transform.parent.parent.name.Contains("Walls"));

            if (isWall)
            {
                if (isAR)
                {
                    // Extend collider height upward to 3.5x so the cube cannot jump over maze walls in AR diorama.
                    // Bottom of collider remains at exact original floor height:
                    // Original: center.y = 0, size.y = 1 -> bottom = -0.5
                    // Scaled: center.y = 1.25, size.y = 3.5 -> bottom = 1.25 - 1.75 = -0.5 (exact floor alignment!)
                    col.size = new Vector3(col.size.x, 3.5f, col.size.z);
                    col.center = new Vector3(col.center.x, 1.25f, col.center.z);
                }
                else
                {
                    // Restore original 1x height for standard 3D mode
                    col.size = new Vector3(col.size.x, 1.0f, col.size.z);
                    col.center = new Vector3(col.center.x, 0.0f, col.center.z);
                }
            }
        }
    }

    void OnDestroy()
    {
        Physics.defaultContactOffset = 0.01f;
        if (player != null)
        {
            var pcCol = player.GetComponent<Collider>();
            if (pcCol != null) pcCol.contactOffset = 0.01f;
        }
    }
}
