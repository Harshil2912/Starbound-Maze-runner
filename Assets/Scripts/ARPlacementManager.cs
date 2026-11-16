using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ARPlacementManager : MonoBehaviour
{
    const string PrefDebugPlaneVisuals = "MR_DebugPlaneVisuals";

    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public ARCameraBackground cameraBackground;
    public GameObject reticleVisual;
    public Transform contentRoot;
    public GameObject shadowCatcherPlane;
    public Camera arCamera;

    [Header("Plane Materials (Debug Transparency)")]
    public Material planeGridMaterial;
    public Material planeBorderMaterial;

    [Header("UI Guidance")]
    public GameObject guidancePanel;
    public Text guidanceText;

    [Header("Placement Configuration")]
    public float defaultScale = 0.020f; // Scales 40m maze to ~0.8m tabletop diorama
    public float minScale = 0.008f;
    public float maxScale = 0.060f;
    [Range(3, 5)] public int maxDetectedPlanes = 4;
    public bool isPlaced { get; private set; }
    public static event System.Action<bool> OnPlacementStateChanged;

    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    static readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    static readonly Color DefaultDotColor = new Color(0.86f, 0.87f, 0.89f, 0.36f);
    static readonly Color DefaultPlaneTintColor = new Color(0.86f, 0.87f, 0.89f, 0.05f);
    static readonly Color DefaultBorderColor = new Color(0.86f, 0.87f, 0.89f, 0.16f);

    static bool debugPrefLoaded;
    static bool debugPlaneVisualsEnabled = true;

    public static bool DebugPlaneVisualsEnabled
    {
        get
        {
            EnsureDebugPrefLoaded();
            return debugPlaneVisualsEnabled;
        }
    }

    Pose placementPose;
    bool poseValid;

    // Gesture tracking for two-finger pinch scale and twist rotate
    float prevTouchDistance;
    float prevTouchAngle;
    int gestureTouchId1 = -1;
    int gestureTouchId2 = -1;
    int lastAppliedPlaneCount = -1;
    bool lastAppliedVisualState = true;
    bool planesSubscribed;
    string lastGuidanceText;
    CubeController cachedCube;
    static PointerEventData cachedPointerData;
    static EventSystem cachedEventSystem;
    readonly HashSet<TrackableId> allowedPlaneIds = new HashSet<TrackableId>();
    readonly HashSet<int> uiStartedTouchIds = new HashSet<int>();

    struct RbLocalSnapshot
    {
        public Rigidbody rb;
        public Vector3 localPos;
        public Vector3 localVel;
    }
    readonly List<RbLocalSnapshot> rbSnapshots = new List<RbLocalSnapshot>(8);

    static void EnsureDebugPrefLoaded()
    {
        if (debugPrefLoaded) return;
        debugPrefLoaded = true;
        debugPlaneVisualsEnabled = PlayerPrefs.GetInt(PrefDebugPlaneVisuals, 1) == 1;
    }

    public static void SetDebugPlaneVisualsEnabled(bool visible)
    {
        debugPrefLoaded = true;
        debugPlaneVisualsEnabled = visible;
        PlayerPrefs.SetInt(PrefDebugPlaneVisuals, visible ? 1 : 0);
        PlayerPrefs.Save();

        var mgr = FindAnyObjectByType<ARPlacementManager>(FindObjectsInactive.Include);
        if (mgr != null)
        {
            mgr.ApplyDebugPlaneVisuals(visible);
        }
    }

    void Awake()
    {
        EnsureDebugPrefLoaded();
        ResolveReferences();
        ApplyDebugPlaneVisuals(DebugPlaneVisualsEnabled);
    }

    void ResolveReferences()
    {
        if (raycastManager == null) raycastManager = GetComponentInParent<ARRaycastManager>();
        if (planeManager == null) planeManager = GetComponentInParent<ARPlaneManager>();
        if (arCamera == null)
        {
            var cam = GetComponentInChildren<Camera>(true);
            if (cam != null) arCamera = cam;
            else arCamera = Camera.main;
        }
        if (cameraBackground == null && arCamera != null)
        {
            cameraBackground = arCamera.GetComponent<ARCameraBackground>();
        }
        if (shadowCatcherPlane == null && contentRoot != null)
        {
            var t = contentRoot.Find("AR_ShadowCatcherPlane");
            if (t != null) shadowCatcherPlane = t.gameObject;
        }
        if (planeManager != null && planeManager.planePrefab != null)
        {
            if (planeGridMaterial == null)
            {
                var mr = planeManager.planePrefab.GetComponent<MeshRenderer>();
                if (mr != null) planeGridMaterial = mr.sharedMaterial;
            }
            if (planeBorderMaterial == null)
            {
                var lr = planeManager.planePrefab.GetComponent<LineRenderer>();
                if (lr != null) planeBorderMaterial = lr.sharedMaterial;
            }
        }
    }

    void SubscribePlaneEvents()
    {
        if (!planesSubscribed && planeManager != null)
        {
            planeManager.trackablesChanged.AddListener(OnPlanesChanged);
            planesSubscribed = true;
        }
    }

    void UnsubscribePlaneEvents()
    {
        if (planesSubscribed && planeManager != null)
        {
            planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
            planesSubscribed = false;
        }
    }

    void OnEnable()
    {
        EnsureDebugPrefLoaded();
        ResolveReferences();
        SubscribePlaneEvents();
        isPlaced = false;
        OnPlacementStateChanged?.Invoke(false);
        poseValid = false;
        lastAppliedPlaneCount = -1;
        lastGuidanceText = null;
        prevTouchDistance = 0f;
        gestureTouchId1 = -1;
        gestureTouchId2 = -1;
        allowedPlaneIds.Clear();
        uiStartedTouchIds.Clear();
        if (cachedCube != null) cachedCube.SuppressFallDamage(2.5f);

        if (raycastManager != null) raycastManager.enabled = true;
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            planeManager.enabled = true;
        }
        if (contentRoot != null) contentRoot.gameObject.SetActive(false);
        if (shadowCatcherPlane != null) shadowCatcherPlane.SetActive(false);
        if (reticleVisual != null) reticleVisual.SetActive(false);
        if (guidancePanel != null) guidancePanel.SetActive(true);
        SyncPlaneCapsAndVisuals();
        ApplyDebugPlaneVisuals(DebugPlaneVisualsEnabled);
    }

    void OnDisable()
    {
        UnsubscribePlaneEvents();
        if (reticleVisual != null) reticleVisual.SetActive(false);
        if (guidancePanel != null) guidancePanel.SetActive(false);
        if (shadowCatcherPlane != null) shadowCatcherPlane.SetActive(false);
        StopPlaneDetection();
    }

    void OnDestroy()
    {
        UnsubscribePlaneEvents();
    }

    void OnApplicationQuit()
    {
#if UNITY_EDITOR
        // Keep project material assets at visible defaults in Editor when stopping Play Mode
        SetMaterialVisualState(true);
#endif
    }

    public void ApplyDebugPlaneVisuals(bool visible)
    {
        ResolveReferences();
        SetMaterialVisualState(visible);

        if (planeManager != null)
        {
            Color borderCol = visible ? DefaultBorderColor : new Color(DefaultBorderColor.r, DefaultBorderColor.g, DefaultBorderColor.b, 0f);
            foreach (var plane in planeManager.trackables)
            {
                if (plane == null) continue;
                var mr = plane.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null)
                {
                    ApplyToGridMaterial(mr.sharedMaterial, visible);
                }
                var lr = plane.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.startColor = borderCol;
                    lr.endColor = borderCol;
                    if (lr.sharedMaterial != null) ApplyToBorderMaterial(lr.sharedMaterial, visible);
                }
            }
            lastAppliedPlaneCount = allowedPlaneIds.Count;
            lastAppliedVisualState = visible;
        }
    }

    void SetMaterialVisualState(bool visible)
    {
        ApplyToGridMaterial(planeGridMaterial, visible);
        ApplyToBorderMaterial(planeBorderMaterial, visible);
    }

    static void ApplyToGridMaterial(Material mat, bool visible)
    {
        if (mat == null) return;
        Color dot = DefaultDotColor;
        dot.a = visible ? DefaultDotColor.a : 0f;
        Color tint = DefaultPlaneTintColor;
        tint.a = visible ? DefaultPlaneTintColor.a : 0f;

        if (mat.HasProperty("_Color")) mat.SetColor("_Color", dot);
        if (mat.HasProperty("_GridColor")) mat.SetColor("_GridColor", dot);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", dot);
        if (mat.HasProperty("_PlaneColor")) mat.SetColor("_PlaneColor", tint);
        if (mat.HasProperty("_AlbedoAlpha")) mat.SetFloat("_AlbedoAlpha", visible ? 1f : 0f);
    }

    static void ApplyToBorderMaterial(Material mat, bool visible)
    {
        if (mat == null) return;
        Color b = DefaultBorderColor;
        b.a = visible ? DefaultBorderColor.a : 0f;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", b);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", b);
    }

    public void ClearAllPlanes()
    {
        ResolveReferences();
        isPlaced = false;
        poseValid = false;
        lastAppliedPlaneCount = -1;
        lastGuidanceText = null;
        allowedPlaneIds.Clear();

        if (planeManager != null)
        {
            var toDestroy = new List<GameObject>();
            foreach (var plane in planeManager.trackables)
            {
                if (plane != null) toDestroy.Add(plane.gameObject);
            }
            for (int i = 0; i < toDestroy.Count; i++)
            {
                if (toDestroy[i] != null) Destroy(toDestroy[i]);
            }
        }

        // Also sweep any leftover ARPlane children under XR Origin
        var root = transform.root;
        if (root != null)
        {
            var leftover = root.GetComponentsInChildren<ARPlane>(true);
            for (int i = 0; i < leftover.Length; i++)
            {
                if (leftover[i] != null) Destroy(leftover[i].gameObject);
            }
        }
    }

    void StopPlaneDetection()
    {
        if (planeManager != null)
        {
            if (planeManager.enabled)
                planeManager.requestedDetectionMode = PlaneDetectionMode.None;
            planeManager.enabled = false;
        }
        if (raycastManager != null)
        {
            raycastManager.enabled = false;
        }
        SetPlanesActive(false);
    }

    void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        foreach (var kv in args.removed)
        {
            allowedPlaneIds.Remove(kv.Key);
        }

        if (isPlaced)
        {
            foreach (var p in args.added)
                if (p != null) p.gameObject.SetActive(false);
            foreach (var p in args.updated)
                if (p != null) p.gameObject.SetActive(false);
            StopPlaneDetection();
            return;
        }

        int cap = Mathf.Clamp(maxDetectedPlanes, 3, 5);
        foreach (var p in args.added)
        {
            if (p == null) continue;
            if (p.subsumedBy != null)
            {
                p.gameObject.SetActive(false);
                continue;
            }
            if (allowedPlaneIds.Count < cap)
            {
                allowedPlaneIds.Add(p.trackableId);
                p.gameObject.SetActive(true);
            }
            else
            {
                p.gameObject.SetActive(false);
            }
        }

        foreach (var p in args.updated)
        {
            if (p == null) continue;
            if (p.subsumedBy != null)
            {
                allowedPlaneIds.Remove(p.trackableId);
                p.gameObject.SetActive(false);
                continue;
            }
            if (allowedPlaneIds.Contains(p.trackableId))
            {
                p.gameObject.SetActive(true);
            }
            else if (allowedPlaneIds.Count < cap)
            {
                allowedPlaneIds.Add(p.trackableId);
                p.gameObject.SetActive(true);
            }
            else
            {
                p.gameObject.SetActive(false);
            }
        }

        EnforceScanningCap();
        ApplyDebugPlaneVisuals(DebugPlaneVisualsEnabled);
    }

    void SyncPlaneCapsAndVisuals()
    {
        if (planeManager == null) return;
        int cap = Mathf.Clamp(maxDetectedPlanes, 3, 5);
        foreach (var plane in planeManager.trackables)
        {
            if (plane == null) continue;
            if (plane.subsumedBy != null || isPlaced)
            {
                plane.gameObject.SetActive(false);
                continue;
            }
            if (allowedPlaneIds.Contains(plane.trackableId))
            {
                plane.gameObject.SetActive(true);
            }
            else if (allowedPlaneIds.Count < cap)
            {
                allowedPlaneIds.Add(plane.trackableId);
                plane.gameObject.SetActive(true);
            }
            else
            {
                plane.gameObject.SetActive(false);
            }
        }
        EnforceScanningCap();
    }

    void EnforceScanningCap()
    {
        if (planeManager == null || !planeManager.enabled) return;
        int cap = Mathf.Clamp(maxDetectedPlanes, 3, 5);
        PlaneDetectionMode desired = (allowedPlaneIds.Count >= cap)
            ? PlaneDetectionMode.None
            : PlaneDetectionMode.Horizontal;
        if (planeManager.requestedDetectionMode != desired)
        {
            planeManager.requestedDetectionMode = desired;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f || ControlsLayoutManager.IsEditingLayout)
        {
            if (guidancePanel != null && guidancePanel.activeSelf) guidancePanel.SetActive(false);
            return;
        }

        if (DualModeManager.Instance != null && DualModeManager.Instance.currentMode != DualModeManager.GameMode.ARTabletop)
        {
            if (guidancePanel != null && guidancePanel.activeSelf) guidancePanel.SetActive(false);
            if (reticleVisual != null && reticleVisual.activeSelf) reticleVisual.SetActive(false);
            return;
        }

        var gameUI = Object.FindFirstObjectByType<GameUI>();
        if (gameUI != null && (gameUI.IsMenuOpen || gameUI.IsPaused))
        {
            if (guidancePanel != null && guidancePanel.activeSelf) guidancePanel.SetActive(false);
            if (reticleVisual != null && reticleVisual.activeSelf) reticleVisual.SetActive(false);
            return;
        }

        if (!isPlaced)
        {
            if (guidancePanel != null && !guidancePanel.activeSelf)
                guidancePanel.SetActive(true);

            // Sync newly discovered planes with debug plane visual state
            if (planeManager != null &&
                (allowedPlaneIds.Count != lastAppliedPlaneCount || lastAppliedVisualState != DebugPlaneVisualsEnabled))
            {
                SyncPlaneCapsAndVisuals();
                ApplyDebugPlaneVisuals(DebugPlaneVisualsEnabled);
                lastAppliedPlaneCount = allowedPlaneIds.Count;
            }

            UpdatePlacementPose();
            UpdateReticle();
            UpdateGuidanceUI();

            // Tap anywhere on detected surface (center reticle OR direct tap point on a tracked plane)
            if (WasPlacementRequested(out Vector2 tapPos))
            {
                if (!poseValid)
                {
                    TryRaycastAtScreenPos(tapPos);
                }
                if (poseValid)
                {
                    PlaceContent();
                }
            }
        }
        else
        {
            HandleGestures();
        }
    }

    bool TryRaycastAtScreenPos(Vector2 screenPos)
    {
        if (raycastManager != null)
        {
            if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            {
                poseValid = true;
                placementPose = hits[0].pose;
                return true;
            }
            if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinBounds))
            {
                poseValid = true;
                placementPose = hits[0].pose;
                return true;
            }
            if (planeManager != null && planeManager.trackables.count > 0 &&
                raycastManager.Raycast(screenPos, hits, TrackableType.PlaneEstimated))
            {
                poseValid = true;
                placementPose = hits[0].pose;
                return true;
            }
        }

#if UNITY_EDITOR
        if (arCamera != null)
        {
            Ray ray = arCamera.ScreenPointToRay(screenPos);
            Plane simTablePlane = new Plane(Vector3.up, new Vector3(0f, -0.5f, 0f));
            if (simTablePlane.Raycast(ray, out float enter))
            {
                poseValid = true;
                placementPose = new Pose(ray.GetPoint(enter), Quaternion.identity);
                return true;
            }
        }
#endif
        return false;
    }

    void UpdatePlacementPose()
    {
        poseValid = false;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        TryRaycastAtScreenPos(screenCenter);
    }

    void UpdateReticle()
    {
        if (reticleVisual == null) return;

        if (poseValid && !isPlaced)
        {
            reticleVisual.SetActive(true);
            reticleVisual.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
        else
        {
            reticleVisual.SetActive(false);
        }
    }

    void UpdateGuidanceUI()
    {
        if (guidanceText == null) return;

        string msg;
#if !UNITY_EDITOR
        if (ARSession.state == ARSessionState.Unsupported)
        {
            msg = "ARCore unsupported on this device. Tap VIEW button for 3D Normal.";
            SetGuidanceMessage(msg);
            return;
        }
        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            msg = "Google Play Services for AR required / updating...";
            SetGuidanceMessage(msg);
            return;
        }
        if (cameraBackground != null && !cameraBackground.backgroundRenderingEnabled)
        {
            msg = "Starting AR camera feed... (" + ARSession.state + ")";
            SetGuidanceMessage(msg);
            return;
        }
#endif

        if (poseValid)
        {
            SetGuidanceMessage("SURFACE DETECTED! Tap screen to place maze diorama");
            return;
        }

        int planeCount = Mathf.Min(Mathf.Clamp(maxDetectedPlanes, 3, 5), allowedPlaneIds.Count);
        if (planeCount > 0)
        {
            msg = DebugPlaneVisualsEnabled
                ? planeCount + " surface(s) found — aim center at dotted surface & tap"
                : planeCount + " surface(s) found — aim center at surface & tap to place";
        }
        else
        {
            msg = "Move device slowly over a lit floor or table to scan...";
        }
        SetGuidanceMessage(msg);
    }

    void SetGuidanceMessage(string msg)
    {
        if (msg == lastGuidanceText) return;
        lastGuidanceText = msg;
        guidanceText.text = msg;
    }

    static bool IsPointerOverUI(Vector2 screenPos)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        if (cachedPointerData == null || cachedEventSystem != es)
        {
            cachedEventSystem = es;
            cachedPointerData = new PointerEventData(es);
        }
        cachedPointerData.position = screenPos;
        uiHits.Clear();
        es.RaycastAll(cachedPointerData, uiHits);
        return uiHits.Count > 0;
    }

    bool WasPlacementRequested(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        var ts = Touchscreen.current;
        if (ts != null)
        {
            var touches = ts.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                if (t.press.wasPressedThisFrame)
                {
                    Vector2 pos = t.position.ReadValue();
                    if (!IsPointerOverUI(pos))
                    {
                        screenPos = pos;
                        return true;
                    }
                    return false;
                }
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = mouse.position.ReadValue();
            if (!IsPointerOverUI(pos))
            {
                screenPos = pos;
                return true;
            }
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (!IsPointerOverUI(touch.position))
                {
                    screenPos = touch.position;
                    return true;
                }
                return false;
            }
        }
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = Input.mousePosition;
            if (!IsPointerOverUI(pos))
            {
                screenPos = pos;
                return true;
            }
        }
#endif
#endif
        return false;
    }

    public void PlaceContent()
    {
        if (!poseValid || contentRoot == null) return;

        contentRoot.position = placementPose.position;

        // Face the maze diorama towards the camera initially
        if (arCamera != null)
        {
            Vector3 lookDir = arCamera.transform.position - placementPose.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                contentRoot.rotation = Quaternion.LookRotation(lookDir);
            }
            else
            {
                contentRoot.rotation = placementPose.rotation;
            }
        }
        else
        {
            contentRoot.rotation = placementPose.rotation;
        }

        contentRoot.localScale = Vector3.one * defaultScale;
        Physics.SyncTransforms();
        contentRoot.gameObject.SetActive(true);
        if (shadowCatcherPlane != null) shadowCatcherPlane.SetActive(true);

        var dmm = DualModeManager.Instance;
        if (dmm != null) dmm.SyncPhysicsContactOffset(defaultScale);
        if (cachedCube == null) cachedCube = contentRoot.GetComponentInChildren<CubeController>(true);

        var rbs = contentRoot.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs)
        {
            if (rb == null) continue;
            Quaternion targetRot = rb.transform.parent != null ? rb.transform.parent.rotation : contentRoot.rotation;
            if (!rb.isKinematic)
            {
                var mode = rb.interpolation;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = rb.transform.position;
                rb.rotation = targetRot;
                rb.transform.rotation = targetRot;
                rb.interpolation = mode;
            }
        }
        Physics.SyncTransforms();
        if (cachedCube != null)
        {
            cachedCube.SuppressFallDamage(2.5f);
            cachedCube.SyncScaleDependentEffects(defaultScale, forceClearTrail: true);
            cachedCube.NotifyParentTransformSynced();
        }

        isPlaced = true;
        OnPlacementStateChanged?.Invoke(true);
        prevTouchDistance = 0f;
        gestureTouchId1 = -1;
        gestureTouchId2 = -1;
        if (reticleVisual != null) reticleVisual.SetActive(false);
        if (guidancePanel != null) guidancePanel.SetActive(false);

        // Stop surface detection completely and hide plane visualizers once maze is placed
        StopPlaneDetection();

        // Apply holographic shader across Base & active level ONLY after AR placement
        var ahm = ARHologramManager.Instance;
        if (ahm != null)
        {
            ahm.SetHologramMode(true);
        }

        GameAudioManager.Instance?.PlayARPlace();
        HapticsManager.Pickup();
    }

    public void Reposition()
    {
        ResolveReferences();
        SubscribePlaneEvents();
        isPlaced = false;
        OnPlacementStateChanged?.Invoke(false);
        poseValid = false;
        lastAppliedPlaneCount = -1;
        lastGuidanceText = null;
        prevTouchDistance = 0f;
        gestureTouchId1 = -1;
        gestureTouchId2 = -1;
        allowedPlaneIds.Clear();
        uiStartedTouchIds.Clear();
        if (cachedCube != null) cachedCube.SuppressFallDamage(2.5f);

        if (contentRoot != null) contentRoot.gameObject.SetActive(false);
        if (shadowCatcherPlane != null) shadowCatcherPlane.SetActive(false);
        if (arHologramOffRequired())
        {
            var ahm = ARHologramManager.Instance;
            if (ahm != null) ahm.SetHologramMode(false);
        }
        if (guidancePanel != null) guidancePanel.SetActive(true);

        if (planeManager != null && planeManager.trackables.count >= Mathf.Clamp(maxDetectedPlanes, 3, 5))
        {
            ClearAllPlanes();
            var dmm = DualModeManager.Instance;
            if (dmm != null && dmm.arSession != null)
            {
                var session = dmm.arSession.GetComponent<ARSession>();
                if (session != null && session.enabled) session.Reset();
            }
        }

        if (raycastManager != null) raycastManager.enabled = true;
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            planeManager.enabled = true;
        }
        SyncPlaneCapsAndVisuals();
        ApplyDebugPlaneVisuals(DebugPlaneVisualsEnabled);
    }

    bool arHologramOffRequired()
    {
        return ARHologramManager.Instance != null && ARHologramManager.Instance.isHologramActive;
    }

    public void CancelPlacementUI()
    {
        isPlaced = false;
        poseValid = false;
        lastGuidanceText = null;
        if (reticleVisual != null) reticleVisual.SetActive(false);
        if (guidancePanel != null) guidancePanel.SetActive(false);
        if (shadowCatcherPlane != null) shadowCatcherPlane.SetActive(false);
        StopPlaneDetection();
    }

    public void SetPlanesActive(bool active)
    {
        if (planeManager == null) return;
        foreach (var plane in planeManager.trackables)
        {
            if (plane != null) plane.gameObject.SetActive(active);
        }
    }

    void HandleGestures()
    {
        if (contentRoot == null) return;

#if ENABLE_INPUT_SYSTEM && UNITY_EDITOR
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.01f)
            {
                ApplyDioramaScale(contentRoot.localScale.x * (1f + Mathf.Sign(scrollY) * 0.08f));
            }
        }
#endif

#if ENABLE_INPUT_SYSTEM
        var ts = Touchscreen.current;
        if (ts == null) return;

        int activeCount = 0;
        Vector2 p1 = Vector2.zero, p2 = Vector2.zero;
        int id1 = -1, id2 = -1;
        bool b1 = false, b2 = false;
        var touches = ts.touches;
        for (int i = 0; i < touches.Count; i++)
        {
            var t = touches[i];
            int tid = t.touchId.ReadValue();
            if (!t.isInProgress)
            {
                uiStartedTouchIds.Remove(tid);
                continue;
            }

            Vector2 pos = t.position.ReadValue();
            if (t.press.wasPressedThisFrame && IsPointerOverUI(pos))
            {
                uiStartedTouchIds.Add(tid);
            }
            if (uiStartedTouchIds.Contains(tid)) continue;

            if (activeCount == 0)
            {
                p1 = pos;
                id1 = tid;
                b1 = t.press.wasPressedThisFrame;
                activeCount = 1;
            }
            else if (activeCount == 1)
            {
                p2 = pos;
                id2 = tid;
                b2 = t.press.wasPressedThisFrame;
                activeCount = 2;
                break;
            }
        }
        if (activeCount < 2)
        {
            prevTouchDistance = 0f;
            gestureTouchId1 = -1;
            gestureTouchId2 = -1;
            return;
        }

        float currentDist = Vector2.Distance(p1, p2);
        Vector2 delta = p2 - p1;
        float currentAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        bool idsChanged = (id1 != gestureTouchId1 || id2 != gestureTouchId2);
        gestureTouchId1 = id1;
        gestureTouchId2 = id2;

        if (b1 || b2 || idsChanged || prevTouchDistance <= 1f)
        {
            prevTouchDistance = currentDist;
            prevTouchAngle = currentAngle;
            return;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount != 2)
        {
            prevTouchDistance = 0f;
            return;
        }

        Touch t1 = Input.GetTouch(0);
        Touch t2 = Input.GetTouch(1);
        if ((t1.phase == TouchPhase.Began && IsPointerOverUI(t1.position)) ||
            (t2.phase == TouchPhase.Began && IsPointerOverUI(t2.position)) ||
            uiStartedTouchIds.Contains(t1.fingerId) || uiStartedTouchIds.Contains(t2.fingerId))
        {
            if (t1.phase == TouchPhase.Began && IsPointerOverUI(t1.position)) uiStartedTouchIds.Add(t1.fingerId);
            if (t2.phase == TouchPhase.Began && IsPointerOverUI(t2.position)) uiStartedTouchIds.Add(t2.fingerId);
            prevTouchDistance = 0f;
            return;
        }

        float currentDist = Vector2.Distance(t1.position, t2.position);
        Vector2 delta = t2.position - t1.position;
        float currentAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began || prevTouchDistance <= 1f)
        {
            prevTouchDistance = currentDist;
            prevTouchAngle = currentAngle;
            return;
        }
#else
        return;
#endif

        // Pinch to zoom in / zoom out
        if (prevTouchDistance > 60f && currentDist > 60f && Mathf.Abs(currentDist - prevTouchDistance) > 0.4f)
        {
            float scaleFactor = Mathf.Clamp(currentDist / prevTouchDistance, 0.85f, 1.15f);
            ApplyDioramaScale(contentRoot.localScale.x * scaleFactor);
        }

        // Twist to rotate around vertical table axis (ignore spikes from finger-order swaps)
        float deltaAngle = Mathf.DeltaAngle(prevTouchAngle, currentAngle);
        if (prevTouchDistance > 60f && currentDist > 60f && Mathf.Abs(deltaAngle) > 0.25f && Mathf.Abs(deltaAngle) < 25f)
        {
            CaptureDynamicRigidbodies();
            contentRoot.Rotate(Vector3.up, -deltaAngle, Space.World);
            Physics.SyncTransforms();
            RestoreDynamicRigidbodies();
        }

        prevTouchDistance = currentDist;
        prevTouchAngle = currentAngle;
    }

    void CaptureDynamicRigidbodies()
    {
        rbSnapshots.Clear();
        if (contentRoot == null) return;
        var rbs = contentRoot.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++)
        {
            var rb = rbs[i];
            if (rb == null || rb.isKinematic) continue;
            rbSnapshots.Add(new RbLocalSnapshot
            {
                rb = rb,
                localPos = contentRoot.InverseTransformPoint(rb.position),
                localVel = contentRoot.InverseTransformVector(rb.linearVelocity)
            });
        }
    }

    void RestoreDynamicRigidbodies()
    {
        if (contentRoot == null) return;
        for (int i = 0; i < rbSnapshots.Count; i++)
        {
            var snap = rbSnapshots[i];
            if (snap.rb == null) continue;
            Vector3 worldPos = contentRoot.TransformPoint(snap.localPos);
            Quaternion targetRot = snap.rb.transform.parent != null ? snap.rb.transform.parent.rotation : contentRoot.rotation;
            var mode = snap.rb.interpolation;
            snap.rb.interpolation = RigidbodyInterpolation.None;
            snap.rb.position = worldPos;
            snap.rb.transform.position = worldPos;
            snap.rb.rotation = targetRot;
            snap.rb.transform.rotation = targetRot;
            snap.rb.linearVelocity = contentRoot.TransformVector(snap.localVel);
            snap.rb.angularVelocity = Vector3.zero;
            snap.rb.interpolation = mode;
        }
        rbSnapshots.Clear();
        Physics.SyncTransforms();
        if (cachedCube == null) cachedCube = contentRoot.GetComponentInChildren<CubeController>(true);
        if (cachedCube != null) cachedCube.NotifyParentTransformSynced();
    }

    void ApplyDioramaScale(float rawScale)
    {
        if (contentRoot == null) return;
        float newScale = Mathf.Clamp(rawScale, minScale, maxScale);
        if (Mathf.Abs(newScale - contentRoot.localScale.x) < 0.00005f) return;

        CaptureDynamicRigidbodies();
        contentRoot.localScale = Vector3.one * newScale;
        var dmm = DualModeManager.Instance;
        if (dmm != null) dmm.SyncPhysicsContactOffset(newScale);
        Physics.SyncTransforms();
        RestoreDynamicRigidbodies();
        if (cachedCube != null) cachedCube.SyncScaleDependentEffects(newScale, forceClearTrail: false);
        var ahm = ARHologramManager.Instance;
        if (ahm != null && ahm.isHologramActive) ahm.UpdateHologramScale(newScale);
    }
}
