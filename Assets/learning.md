 
# Maze Runner: Engineering Knowledge Base & AR Post-Mortem (`learning.md`)

This document serves as an exhaustive, technical learning review of all problems, architectural decisions, mathematical derivations, and debugging solutions implemented during the transformation of **Maze Runner** from a standard 3D game into a production-grade **2-Way Dual-Mode (Normal 3D + AR Tabletop Diorama)** mobile game in **Unity 6 (6000.3.24f1)** with **URP 17.3.0** and **Google ARCore**.

Use this guide to review lessons learned, avoid common pitfalls, and reference proven design patterns for future AR and mobile game projects.

---

## Table of Contents
1. [Android Manifest & Launcher Activity Visibility Trap (Unity 6 GameActivity)](#1-android-manifest--launcher-activity-visibility-trap-unity-6-gameactivity)
2. [ARCore Hardware Feature Lockdown & Dual-Mode Philosophy](#2-arcore-hardware-feature-lockdown--dual-mode-philosophy)
3. [Vulkan Minimum SDK Version Requirement (API Level 29)](#3-vulkan-minimum-sdk-version-requirement-api-level-29)
4. [Runtime Camera Permission Workflow on Android](#4-runtime-camera-permission-workflow-on-android)
5. [Dual-Rig Camera Architecture vs Single-Camera Blending](#5-dual-rig-camera-architecture-vs-single-camera-blending)
6. [Multi-Level Tabletop Center Alignment (Pivot Offset Pattern)](#6-multi-level-tabletop-center-alignment-pivot-offset-pattern)
7. [The Miniaturized Physics Paradox (Jump Velocity Scaling)](#7-the-miniaturized-physics-paradox-jump-velocity-scaling)
8. [Scale-Adaptive Gameplay Triggers & Detection Radii](#8-scale-adaptive-gameplay-triggers--detection-radii)
9. [Holographic Glass Shader Architecture & Subtle Lighting Calibration](#9-holographic-glass-shader-architecture--subtle-lighting-calibration)
10. [Shadow Catcher Plane Isolation & Immersion Grounding](#10-shadow-catcher-plane-isolation--immersion-grounding)
11. [Dynamic UI Synchronization, Safe Area Padding & Orientation Lock](#11-dynamic-ui-synchronization-safe-area-padding--orientation-lock)
12. [Scene Asset Serialization Corruptions & Strict 3D vs AR Visual Decoupling](#12-scene-asset-serialization-corruptions--strict-3d-vs-ar-visual-decoupling)
13. [Physical Surface Tracking, Table Detection & Interactive Reticle Placement Pipeline](#13-physical-surface-tracking-table-detection--interactive-reticle-placement-pipeline)
14. [AR Troubleshooting Post-Mortem: Why Earlier AR Failed & What Finally Made It Work](#14-ar-troubleshooting-post-mortem-why-earlier-ar-failed--what-finally-made-it-work)
15. [AR World-Space Scale Traps (TrailRenderer, Physics Contact Offset, Jump Feel & Lasers)](#15-ar-world-space-scale-traps-trailrenderer-physics-contact-offset-jump-feel--lasers)
16. [Category-Aware AR Holograms & GPU-Instanced Visual Halos](#16-category-aware-ar-holograms--gpu-instanced-visual-halos)
17. [Level 4 Death Loop, Hold-to-Move Input Drift, Surface Detection Cap & Mobile Optimization](#17-level-4-death-loop-hold-to-move-input-drift-surface-detection-cap--mobile-optimization)
18. [Post-Polish Physics & Controls Rework: AR Jump Parabolic Arc, Map-Cube Transform Sync & Robust Hold Movement](#18-post-polish-physics--controls-rework-ar-jump-parabolic-arc-map-cube-transform-sync--robust-hold-movement)
19. [Mobile Release & Multi-Mode Polish: Life Loss Prevention, Persistent Core State, AR Hologram Yellow Crate & Mobile UI Cleanliness](#19-mobile-release--multi-mode-polish-life-loss-prevention-persistent-core-state-ar-hologram-yellow-crate--mobile-ui-cleanliness)
20. [AR Visual & Shader Refinement: Scale-Adaptive Hologram Scan, Particle Hierarchy Scaling, Mobile On-Chip MSAA & High-Contrast Alpha Reticle](#20-ar-visual--shader-refinement-scale-adaptive-hologram-scan-particle-hierarchy-scaling-mobile-on-chip-msaa--high-contrast-alpha-reticle)
21. [Gameplay, Hazard & Platform Physics Polish: Hitbox Local Space, Zero-Alloc Contacts, Local Spinner Rotation & Moving Platform Momentum](#21-gameplay-hazard--platform-physics-polish-hitbox-local-space-zero-alloc-contacts-local-spinner-rotation--moving-platform-momentum)

---

## 1. Android Manifest & Launcher Activity Visibility Trap (Unity 6 GameActivity)

### The Problem
When the APK was transferred and installed onto Android devices, Android reported *"App installed successfully"*, but **no app icon appeared anywhere on the phone** (neither on the home screen nor in the app drawer). The user was unable to tap, find, or open the app.

### Root Cause Analysis
In Unity 6 (6000.x), Unity introduced a new setting:
`PlayerSettings.Android.applicationEntry` which defaults to `GameActivity` (`com.unity3d.player.UnityPlayerGameActivity`).

When `applicationEntry` is set to `GameActivity`, Unity's background Gradle manifest merger automatically injects:
```xml
<activity android:name="com.unity3d.player.UnityPlayerActivity"
          android:enabled="false" />
```
Because our custom `AndroidManifest.xml` had bound the launcher intent-filter:
```xml
<intent-filter>
    <action android:name="android.intent.action.MAIN" />
    <category android:name="android.intent.category.LAUNCHER" />
</intent-filter>
```
to `com.unity3d.player.UnityPlayerActivity`, Gradle merged these declarations together. The resulting APK contained:
- `UnityPlayerActivity`: **`android:enabled="false"`** with `category.LAUNCHER`.
- `UnityPlayerGameActivity`: Enabled, but **without any `category.LAUNCHER`**.

**Android OS Rule**: The Android OS `PackageManager` queries for activities that have both `action.MAIN` and `category.LAUNCHER`. Any activity marked `android:enabled="false"` is completely suppressed by Android launchers. Because no enabled activity declared `LAUNCHER`, the app was treated as a background service/library with **zero visible icons**.

### How We Solved It
1. Switched Unity's application entry back to the classic, rock-solid Unity activity:
   ```csharp
   PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
   ```
2. In `Assets/Plugins/Android/AndroidManifest.xml`, explicitly enforced `android:enabled="true"`, `android:exported="true"`, `android:label="@string/app_name"`, and `android:icon="@mipmap/app_icon"` with `tools:replace`:
   ```xml
   <activity android:name="com.unity3d.player.UnityPlayerActivity"
             android:theme="@style/UnityThemeSelector"
             android:label="@string/app_name"
             android:icon="@mipmap/app_icon"
             android:screenOrientation="sensorLandscape"
             android:launchMode="singleTask"
             android:configChanges="mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density"
             android:hardwareAccelerated="false"
             android:enabled="true"
             android:exported="true"
             tools:replace="android:enabled,android:exported,android:screenOrientation">
       <intent-filter>
           <action android:name="android.intent.action.MAIN" />
           <category android:name="android.intent.category.LAUNCHER" />
       </intent-filter>
       <meta-data android:name="unityplayer.UnityActivity" android:value="true" />
   </activity>
   ```
3. Verified the resulting APK with `aapt dump badging` to guarantee `launchable-activity` is resolved with a non-empty label and valid icon.

### Why This Matters
Whenever authoring custom `AndroidManifest.xml` files in modern Unity versions (Unity 2023 / Unity 6+), always check whether `PlayerSettings.Android.applicationEntry` matches the activity declared in your manifest. If using `UnityPlayerActivity`, ensure `applicationEntry` is set to `Activity`, or use `tools:replace="android:enabled"` to prevent Gradle from disabling it.

---

## 2. ARCore Hardware Feature Lockdown & Dual-Mode Philosophy

### The Problem
During build verification, the APK was declaring:
- `uses-feature: name='android.hardware.camera.ar' required=true`
- `uses-feature: name='com.google.ar.core.depth' required=true`

If installed on devices without Google Play Services for AR preinstalled, or on phones without specialized Time-of-Flight (ToF) depth cameras, Android OS either blocked installation or marked the package as incompatible.

### Root Cause Analysis
In `Assets/XR/Settings/ARCoreSettings.asset`, `m_Requirement` and `m_Depth` were set to `0` (`Required`). When set to `Required`, the Google ARCore pre-build processor automatically injects `required="true"` into the merged manifest.

### How We Solved It
For a **2-Way Dual-Mode game**, AR is an **optional enhancement**, not an absolute prerequisite. The base 3D Normal mode must run on 100% of Android devices.
1. In `ARCoreSettings.asset`, updated enum properties:
   - `m_Requirement: 1` (`Optional`)
   - `m_Depth: 1` (`Optional`)
2. In `AndroidManifest.xml`, declared:
   ```xml
   <uses-feature android:name="android.hardware.camera.ar" android:required="false" tools:replace="android:required" />
   <uses-feature android:name="android.hardware.camera" android:required="false" tools:replace="android:required" />
   <uses-feature android:name="android.hardware.camera.autofocus" android:required="false" tools:replace="android:required" />
   <uses-feature android:name="com.google.ar.core.depth" android:required="false" tools:replace="android:required" />

   <application>
       <meta-data android:name="com.google.ar.core" android:value="optional" tools:replace="android:value" />
   </application>
   ```

### Why This Matters
In hybrid/dual-mode titles, hard-requiring hardware features at the OS manifest level shrinks your potential player base. Always mark optional hardware features with `android:required="false"` and check for feature availability programmatically at runtime.

---

## 3. Vulkan Minimum SDK Version Requirement (API Level 29)

### The Problem
During initial APK compilation, the build pipeline threw:
```
BuildFailedException: ARCore Required apps using Vulkan require a minimum SDK version of AndroidApiLevel29. Currently set to AndroidApiLevel25.
```

### Root Cause Analysis
Google ARCore integrates with Vulkan on Android via hardware memory buffers (`AHardwareBuffer`) and synchronization primitives (`VkSemaphore` / `SyncFD`). Google only stabilized Vulkan ARCore passthrough camera rendering starting in **Android 10 (API Level 29)**. Unity's ARCore build preprocessor enforces this requirement whenever the Vulkan graphics API is included in the Player Settings.

### How We Solved It
Updated the minimum SDK version in Unity:
```csharp
PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
```

### Why This Matters
Modern mobile graphics APIs (Vulkan) and optical tracking frameworks (ARCore/ARKit) have strict OS platform dependencies. Setting `minSdkVersion` too low causes build-time assertion failures. Android 10+ represents over 93% of active Android devices today, making API 29 a safe and stable target.

---

## 4. Runtime Camera Permission Workflow on Android

### The Problem
When the user launched the game and switched to AR Mode, the camera feed remained black or failed to initialize, and Android never asked the user for camera permission.

### Root Cause Analysis
Because ARCore was configured as `Optional`, the Android OS does not request camera permission during app installation. Under Android's permission model (API 23+), dangerous permissions like `CAMERA` must be explicitly requested by the application at runtime. If code attempts to start an `ARSession` before permission is granted, the camera subsystem enters a failed state.

### How We Solved It
Implemented an asynchronous runtime permission request coroutine in `DualModeManager.cs`:
```csharp
#if UNITY_ANDROID && !UNITY_EDITOR
isRequestingPermission = true;
if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
{
    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
    float waitTime = 10f;
    while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera) && waitTime > 0f)
    {
        waitTime -= Time.unscaledDeltaTime;
        yield return null;
    }
}
isRequestingPermission = false;

if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
{
    Debug.LogWarning("[DualModeManager] Camera permission denied by user. Reverting to 3D Normal mode.");
    SetMode(GameMode.Normal3D, true);
    yield break;
}
#endif
```

### Why This Matters
Never activate an AR camera session without first verifying authorization. Always handle permission denial gracefully by falling back to the standard 3D game mode rather than leaving the user with a frozen black screen.

---

## 5. Dual-Rig Camera Architecture vs Single-Camera Blending

### The Problem
In early design iterations, attempting to use a single Camera for both 3D third-person follow mechanics and AR Foundation tracking caused severe projection glitches, aspect ratio clipping, and matrix conflicts.

### Root Cause Analysis
- **Standard 3D Camera**: Controlled by virtual orbit scripts (`ThirdPersonCamera.cs`), virtual field-of-view (FOV), and virtual audio listeners.
- **AR Camera**: Controlled by optical tracking (`ARTrackedPoseDriver`), physical lens focal length intrinsics, hardware camera passthrough shaders (`ARCameraBackground`), and real-world 6-DOF device movement.
Attempting to dynamically strip and add AR Foundation components to a single camera during runtime leads to dirty internal matrices and missing frame buffers.

### How We Solved It
Engineered a clean **Dual-Rig Architecture**:
```
Scene
├── NormalCameraRig (Standard Camera, ThirdPersonCamera follow logic, AudioListener)
└── XROriginRig (XR Origin, AR Camera, ARCameraBackground, ARTrackedPoseDriver)
```
In `DualModeManager.cs`:
- **When entering 3D Mode**: Deactivate `XROriginRig` and `ARSession`, activate `NormalCameraRig`, set `MainCamera` tag, and restore world-aligned character controls.
- **When entering AR Mode**: Deactivate `NormalCameraRig`, activate `ARSession` and `XROriginRig`, set `MainCamera` tag on the AR Camera, and switch character controls to camera-relative movement (`Vector3.ProjectOnPlane(cameraForward, Vector3.up)`).

### Why This Matters
Separation of concerns is paramount in hybrid games. Let AR Foundation manage the optical AR camera, and let your custom camera rig manage standard gameplay. Toggle GameObject hierarchies rather than mutating component states on a shared camera.

---

## 6. Multi-Level Tabletop Center Alignment (Pivot Offset Pattern)

### The Problem
The game consists of 5 levels arranged linearly along the world Z-axis:
- Level 1: $Z = 0$
- Level 2: $Z = 100$
- Level 3: $Z = 200$
- Level 4: $Z = 400$
- Level 5: $Z = 600$

In standard 3D mode, the player travels forward through world coordinates. However, in AR Tabletop mode, placing the content root onto a coffee table at $Z = 0$ meant that when the player advanced to Level 2 ($Z = 100$) or Level 5 ($Z = 600$), the maze would spawn **2.5 to 15 meters away from the table** (in the user's kitchen or through a wall!).

### How We Solved It
We introduced the **Pivot Offset Compensation Pattern** using an intermediate anchor:
```
ARContentRoot (Scaled to 0.025, Anchored to physical table pose)
└── ActiveLevelAnchor (Dynamic Local Offset)
    ├── Level1 (localPos: 0, 0, 0)
    ├── Level2 (localPos: 0, 0, 100)
    ├── Level3 (localPos: 0, 0, 200)
    ├── Level4 (localPos: 0, 0, 400)
    ├── Level5 (localPos: 0, 0, 600)
    └── PlayerCube
```
When `LevelManager` transitions to Level $N$, `DualModeManager.OnLevelEntered(n)` calculates the compensatory vector:
$$\vec{P}_{\text{anchor}} = -\vec{P}_{\text{level}}$$

```csharp
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
}
```

### Why This Matters
Never modify the raw world-space geometry coordinates of existing levels to fit AR. Retain your original level designs and use an intermediate parent anchor with an inverted local offset to bring any level directly to $(0, 0, 0)$ on the player's tabletop.

---

## 7. The Miniaturized Physics Paradox (Jump Velocity Scaling)

### The Problem
To fit an entire $40\text{ m} \times 40\text{ m}$ maze onto a $1\text{ m} \times 1\text{ m}$ tabletop, the AR content root is scaled down by $1:40$ ($S = 0.025$).
However, when the player attempted to jump over blocks in AR mode, the jump was infinitesimally small—the cube barely hopped a millimeter off the ground!

### The Physics Derivation
Why did naive linear velocity scaling fail?
In real-world kinematics:
$$v^2 = 2 g h \implies h = \frac{v^2}{2 g}$$
In Unity, **gravity ($g$) is an absolute world acceleration constant** ($-9.81\text{ m/s}^2$). When you scale a GameObject hierarchy by $S = 0.025$, gravity is **not** scaled by Unity's physics engine; gravity still pulls down at $9.81\text{ m/s}^2$.

- If jump velocity is scaled linearly ($v_{\text{AR}} = S \cdot v_{\text{normal}}$):
  $$h_{\text{AR}} = \frac{(S \cdot v)^2}{2 g} = S^2 \cdot \frac{v^2}{2 g} = S^2 \cdot h_{\text{normal}}$$
  At $S = 0.025$, $S^2 = 0.000625$! The jump height is reduced by a factor of **1,600**, resulting in almost zero upward movement!

- For the jump height to scale **proportionally** to the maze ($h_{\text{AR}} = S \cdot h_{\text{normal}}$):
  $$\frac{v_{\text{AR}}^2}{2 g} = S \cdot \frac{v^2}{2 g} \implies v_{\text{AR}}^2 = S \cdot v^2 \implies \mathbf{v_{\text{AR}} = v_{\text{normal}} \cdot \sqrt{S}}$$

### How We Solved It
In `CubeController.cs`, we implemented scale-aware physics:
```csharp
float s = transform.lossyScale.x;
// Linear movement scales with S
Vector3 targetVelocity = moveDir * (moveSpeed * s);

// Jump velocity MUST scale with sqrt(S) to preserve parabolic arc height in uniform gravity!
float effectiveJumpVelocity = jumpVelocity * Mathf.Sqrt(s);

// Ground check distance scales with S
float checkDist = (0.5f * s) + (0.1f * s);
```

### Why This Matters
This is a fundamental law of physics simulation in AR:
- Linear velocity scales linearly with $S$.
- Jump impulses against uniform gravity scale with $\mathbf{\sqrt{S}}$.
Failing to account for the square root creates broken, unplayable jump mechanics in miniaturized AR dioramas.

---

## 8. Scale-Adaptive Gameplay Triggers & Detection Radii

### The Problem
In AR mode, pickups (hearts, energy cores, shields) could not be collected unless the player cube clipped directly into the center of their origin. Spike traps failed to damage the player, and turrets stopped firing.

### Root Cause Analysis
Throughout the codebase, distance checks were hardcoded in world-space meters:
```csharp
// Broken in AR:
if ((player.position - transform.position).sqrMagnitude < 2.25f) // 1.5m radius
```
At $1:40$ scale, a $1.5\text{ m}$ radius in world space is equivalent to **$60\text{ meters}$ in the diorama**, or conversely, a visual $0.5\text{ m}$ diorama radius is only $0.0125\text{ m}$ in world units!

### How We Solved It
We audited and updated 9 gameplay scripts:
1. `Checkpoint.cs`: `reachRadius * transform.lossyScale.x`
2. `Teleporter.cs`: `activationDistance * transform.lossyScale.x`
3. `HeartPickup.cs`: `collectDistance * transform.lossyScale.x`
4. `EnergyCore.cs`: `collectDistance * transform.lossyScale.x`
5. `ShieldOrb.cs`: `collectDistance * transform.lossyScale.x`
6. `SpikeTrap.cs`: `triggerRadius * transform.lossyScale.x`
7. `Turret.cs`: `detectionRange * transform.lossyScale.x`
8. `Projectile.cs`: `hitRadius * transform.lossyScale.x`
9. `LaserHazard.cs`: `damageRadius * transform.lossyScale.x`

### Why This Matters
Never use raw float literals for geometric distance queries in scripts that may run under scaled parents. Always multiply detection thresholds by `transform.lossyScale.x`.

---

## 9. Holographic Glass Shader Architecture & Subtle Lighting Calibration

### The Problem
The user requested a holographic glass aesthetic matching reference artwork: a translucent teal diorama that retains the underlying textures of the maze walls, but is **subtle and pleasant** rather than flashy, blinding, or over-bloomed.

### The Shader Implementation (`AR_HologramOverlay.shader`)
We engineered a custom Universal Render Pipeline (URP) shader featuring 5 balanced composite layers:

```hlsl
// 1. Underlying Texture Preservation (72% mix)
half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
half3 texturedBase = tex.rgb * _BaseColor.rgb * 1.4;
half3 surfaceColor = lerp(_BaseColor.rgb, texturedBase, _TextureMix) * ambientShade;

// 2. Soft Ambient Lambert Shading (prevents flat appearance)
float ambientShade = 0.78 + 0.22 * saturate(dot(normalWS, float3(0.2, 0.9, 0.3)));

// 3. Subtle Fresnel Rim Glow (edge definition)
float NdotV = saturate(dot(normalWS, viewDirWS));
float fresnel = pow(1.0 - NdotV, _RimPower);
half3 rimGlow = fresnel * _RimColor.rgb * 0.50;

// 4. Gentle Scanline Sheen Wave (dynamic holographic sheen)
float scan = sin(input.positionWS.z * _ScanFreq - _Time.y * _ScanSpeed);
float scanBeam = pow(saturate(scan), _ScanSharpness) * _ScanIntensity;
half3 beamGlow = scanBeam * _ScanColor.rgb * 0.45;

// 5. Calibrated Composite
half3 finalRGB = surfaceColor + rimGlow + beamGlow;
float alpha = saturate(_HoloAlpha + fresnel * 0.18 + scanBeam * 0.12);
```

### Lighting Calibration Matrix
| Property | Early Draft (Too Bright) | Final Calibration (Balanced & Subtle) | Visual Result |
|---|---|---|---|
| `_RimPower` | 2.5 | **2.8** | Thinner, more precise edge highlight |
| `_RimIntensity` | 0.75 | **0.50** | Soft frosted glass sheen without bloom blowout |
| `_ScanIntensity` | 0.45 | **0.22** | Gentle, passing light wave instead of a blinding beam |
| `_TextureMix` | 0.65 | **0.72** | Retains stone/brick/metallic level texture details |
| `_HoloAlpha` | 0.82 | **0.76** | Semi-translucent; user sees table surface below |

### Why This Matters
In AR, digital objects that are overly bright or pure emissive unlit white look artificial and cause rapid eye fatigue. Incorporating ambient directional shading with subtle Fresnel and high texture detail mix grounds the digital hologram into the physical environment.

---

## 10. Shadow Catcher Plane Isolation & Immersion Grounding

### The Problem
During early AR tests, the entire camera view was covered by a giant, glowing teal polygon.

### Root Cause Analysis
An invisible $80\text{ m} \times 80\text{ m}$ quad (`AR_ShadowCatcherPlane`) was added to capture real-time contact shadows onto the user's table. However, `ARHologramManager.ApplyHologramToScene()` was executing:
```csharp
var renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
```
It iterated over **every** renderer in the scene, replacing the shadow catcher's material with the hologram material! Because the shadow plane was huge, its rim glow and scanlines completely eclipsed the maze.

### How We Solved It
1. Restricted `ARHologramManager` to only target renderers under `ActiveLevelAnchor`.
2. Added explicit exclusion checks:
   ```csharp
   if (r.gameObject.name.Contains("ShadowCatcher") || r.gameObject.name.Contains("AR_ShadowCatcherPlane"))
       continue;
   if (r.sharedMaterial != null && r.sharedMaterial.shader.name.Contains("ShadowCatcher"))
       continue;
   ```
3. Restored `M_AR_ShadowCatcher.mat` using `Custom/AR_ShadowCatcher.shader`:
   ```hlsl
   float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
   Light mainLight = GetMainLight(shadowCoord);
   float shadow = mainLight.shadowAttenuation;
   float shadowStrength = (1.0 - shadow) * _ShadowColor.a;
   return half4(_ShadowColor.rgb, shadowStrength);
   ```

### Why This Matters
Real-time contact shadows are the single most powerful visual cue for grounding an AR diorama onto a physical tabletop. Without shadows, the maze appears to float disconnected in air. Keep shadow catchers strictly separated from visual filter scripts.

---

## 11. Dynamic UI Synchronization, Safe Area Padding & Orientation Lock

### Key Solutions Applied
1. **Dynamic Cube Color Indicator**:
   - Replaced the obsolete, non-functional "Stop" button in the center of the D-pad with `CubeColorIndicator.cs`.
   - Listens to `CubeController.OnColorChanged` and smoothly interpolates the circular indicator's color and executes a scale punch when switching colors (Red, Green, Blue, Yellow).
2. **Zero Button Overlap**:
   - Anchored `Btn_Pause` at top-right $(-40, -40)$.
   - Anchored `Btn_AR_Reposition` at top-right $(-145, -40)$, ensuring a 105px horizontal safety gap.
3. **Safe Area Notch Padding**:
   - Implemented `SafeAreaFitter.cs` on the root Canvas to dynamically adjust `RectTransform` margins according to `Screen.safeArea`, preventing buttons from being hidden under camera pinholes or phone notches.
4. **Triple-Layer Landscape Lock**:
   - **Layer 1 (Project Settings)**: `defaultScreenOrientation: 3` (`LandscapeLeft`), autorotate portrait disabled.
   - **Layer 2 (Android Manifest)**: `android:screenOrientation="sensorLandscape"`.
   - **Layer 3 (Runtime Script)**: `Screen.orientation = ScreenOrientation.LandscapeLeft` in `DualModeManager.Awake()`.

---

## 12. Scene Asset Serialization Corruptions & Strict 3D vs AR Visual Decoupling

### The Problem
During playtesting of 3D Normal mode, the user observed that Level 1 walls, floors, and elements had completely lost their original textures and were displaying a cyan holographic glow—the exact look meant exclusively for AR mode. The original authentic 3D textures (stone walls, textured floors, start/end pads) were gone even when playing in traditional 3D.

### Root Cause Analysis
During earlier development testing of the holographic material swapper, the script assigned dynamically generated hologram materials (`_AR_Holo`) to `Renderer.sharedMaterials` instead of runtime instances, or the scene was saved while the holographic materials were active in the editor hierarchy. 

In Unity, saving a `.unity` scene file while components hold references to temporary or modified materials serializes those references directly into the YAML scene asset on disk. Consequently, 30 mesh renderers across `Level1_Maze`, `StartPad`, `EndPad`, `GoalFlag`, `PlayerCube`, and `Base` had their persistent GUIDs replaced with generated hologram assets. When the scene loaded, Unity instantiated the hologram materials as the default appearance.

### How We Solved It
1. **Scene File Recovery Script**:
   Wrote and executed an automated C# script that queried `ActiveLevelAnchor` and inspected all 2,716 mesh renderers. Every renderer with a serialized `_AR_Holo` material was mapped back to its pristine project asset:
   - `MazeWall` $\rightarrow$ `Assets/Materials/MazeWall.mat`
   - `StartPad` $\rightarrow$ `Assets/Materials/StartPad.mat`
   - `EndPad` $\rightarrow$ `Assets/Materials/EndPad.mat`
   - `GoalFlagRed` $\rightarrow$ `Assets/Materials/GoalFlagRed.mat`
   - `GoalFlagPole` $\rightarrow$ `Assets/Materials/GoalFlagPole.mat`
   - `PlayerCube` $\rightarrow$ `Assets/Materials/PlayerCube_Mat.mat`
   - `Base` $\rightarrow$ `Assets/Materials/BaseGreen.mat`
   - `L5_Wall` $\rightarrow$ `Assets/Materials/L5_Wall.mat`
2. **Verification**:
   Re-scanned all 2,716 renderers in the scene asset, verifying exactly **0 `_AR_Holo` materials** remained serialized.
3. **Runtime-Only Swizzling Policy**:
   Modified `ARHologramManager.cs`:
   - Hologram materials are generated purely in memory via `new Material(hologramShader)` and assigned via `renderer.materials` (runtime instances) rather than `renderer.sharedMaterials`.
   - The original `sharedMaterials` array is safely cached in a dictionary `originalMaterials[Renderer]`.
   - Whenever exiting AR mode, returning to the Main Menu, or initializing 3D Normal mode, `RestoreOriginalMaterials()` is executed immediately, guaranteeing that 3D Normal mode and the Main Menu always render 100% authentic project textures.

### Why This Matters
Never modify `sharedMaterials` in play mode or edit mode if the scene can be saved. Dynamic material swizzling should always use runtime instance arrays (`renderer.materials`), cache original asset references, and implement robust teardown/cleanup logic on mode transitions.

---

## 13. Physical Surface Tracking, Table Detection & Interactive Reticle Placement Pipeline

### The Problem
When the user launched the game and switched to AR mode:
1. No camera permission prompt was shown.
2. The device camera feed (passthrough) was absent or black.
3. No floor or tabletop scanning took place.
4. The full-scale 3D maze remained floating at the world origin $(0, 0, 0)$, clipping directly through the camera.
5. The game did not anchor to physical surfaces upon tapping.

### Root Cause Analysis
Seven distinct architectural deficiencies contributed to this breakdown:
1. **Missing `ARPlacementManager`**: The scene had `XR Origin`, `ARPlaneManager`, and `ARRaycastManager`, but lacked the central placement orchestrator component responsible for plane raycasting, reticle positioning, scaling, and anchoring.
2. **Unassigned Plane Prefab**: In `ARPlaneManager`, `planePrefab` was `null`. When Google ARCore detected horizontal tables or floors, Unity had no visual prefab to instantiate, leaving the user with zero visual confirmation that scanning was occurring.
3. **Missing Placement Reticle**: No visual cursor existed to indicate where the raycast from the screen center intersected with the tracked table surface.
4. **Premature Diorama Visibility**: `ARContentRoot` remained active at $(0, 0, 0)$ with a scale of $1.0$ ($40\text{ m} \times 40\text{ m}$), entirely enveloping the user's view before any plane had been detected or tapped.
5. **URP Background Feature Omission**: `ARBackgroundRendererFeature` was present on `Mobile_Renderer.asset`, but absent on `PC_Renderer.asset` and required explicit validation across all pipeline assets.
6. **Shader Stripping in Android Builds**: The custom shader `Custom/AR_HologramOverlay` was dynamically found at runtime using `Shader.Find()`. Because no static material in the build explicitly referenced it, Unity's build pipeline stripped the shader during APK generation.
7. **Menu & Lifecycle Coupling**: When the user pressed "MODE: AR TABLETOP" from the Main Menu, the app attempted to engage AR while the 2D menu canvas covered the screen, creating confusion between menu previews and active gameplay placement.

### How We Solved It
1. **Engineered `AR_PlaneGrid.shader` and `AR_TablePlane.prefab`**:
   - Built a custom URP shader that renders world-space 8cm grid lines with soft feathering and smooth alpha falloff.
   - Packaged a prefab containing `ARPlane`, `ARPlaneMeshVisualizer`, `MeshFilter`, `MeshRenderer`, and `LineRenderer`, assigned to `ARPlaneManager.planePrefab`.
2. **Engineered `AR_Reticle.shader` and `PlacementReticle`**:
   - Designed a circular HUD reticle with an animated pulsing ring, center alignment dot, and cardinal tick marks.
   - Instantiated `AR_PlacementReticle` in the scene, connected to `ARPlacementManager.reticleVisual`.
3. **Engineered `AR_GuidanceHUD` in Canvas**:
   - Added an anchored top-center dynamic guidance banner with `SafeAreaFitter` integration.
   - Displays real-time status:
     - Pre-detection: *"📱 Move device slowly to scan floor or table surface..."*
     - Plane locked: *"✨ Table surface detected! Tap screen to place maze diorama"*
4. **Architected the AR State Machine (`ARPlacementManager.cs`)**:
   - **Scanning Phase**:
     - `ARContentRoot` is set `activeSelf = false` (maze and player hidden).
     - `ARPlaneManager` trackables and `PlacementReticle` are active.
     - Performs `ARRaycastManager.Raycast` from screen center (`Screen.width * 0.5f, Screen.height * 0.5f`) against `TrackableType.PlaneWithinPolygon`.
     - Reticle tracks the plane intersection point with planar normal orientation.
   - **Placement Phase (On Tap)**:
     - User taps screen (ignoring UI pointer events via `EventSystem.IsPointerOverGameObject`).
     - `ARContentRoot.position` is set to the detected hit pose.
     - `ARContentRoot.rotation` is aligned towards the camera's horizontal gaze direction.
     - `ARContentRoot.localScale` is set to `defaultScale = 0.025f` (scaling the 40m maze to a 1.0m tabletop diorama).
     - `ARContentRoot` is set active.
     - Plane grids, reticle, and guidance HUD are hidden for clean gameplay.
     - `ARHologramManager.ApplyHologramToScene()` dynamically applies the subtle frosted teal holographic overlay **only to the placed diorama**.
   - **Repositioning**:
     - Player can tap the `REPOSITION` button at any time to re-hide the diorama, re-enable plane scanning and reticle, and anchor to a different physical surface.
5. **Shader Stripping Prevention**:
   - Added `Custom/AR_HologramOverlay` directly to `m_AlwaysIncludedShaders` in `ProjectSettings/GraphicsSettings.asset`, guaranteeing it is compiled and bundled into the Android APK.
6. **Decoupled Menu Previews from Gameplay Placement**:
   - Created `DualModeManager.PrepareForMenu()`: Ensures standard 3D camera and authentic project textures are active while browsing the Main Menu.
   - Created `DualModeManager.ApplyCurrentModeToGameplay()`: When the player taps "PLAY" (or starts Level 1–5), the selected mode (3D Normal vs AR Tabletop) seamlessly activates.
   - Connected `Btn_Mode` in the in-game HUD so players can switch between 3D Normal and AR Tabletop in real-time while playing.

### Why This Matters
A production-grade mobile AR experience requires clear user guidance, visible plane detection cues, visual placement confirmation via a reticle, and delayed content instantiation. Spawning large 3D worlds before physical surfaces are tracked causes severe disorientation and breaks AR immersion.

---

## Summary Checklist for Future AR Projects

When building your next AR game:
- [ ] **Dual-Rig**: Separate standard 3D camera from the AR `XR Origin`.
- [ ] **Optional Manifest**: Keep ARCore features optional (`required="false"`) so base modes run universally.
- [ ] **Activity Entry**: Ensure `PlayerSettings.Android.applicationEntry` matches the activity in your manifest and that the launcher activity has `android:enabled="true"` and `android:exported="true"`.
- [ ] **API Level**: Set `minSdkVersion` to at least 29 when using ARCore with Vulkan.
- [ ] **Pivot Anchoring**: Use an intermediate offset anchor to center arbitrarily offset levels onto the table $(0, 0, 0)$.
- [ ] **Jump Kinematics**: Scale jump impulse by $\mathbf{\sqrt{S}}$ to counter uniform gravity.
- [ ] **Trigger Radii**: Multiply all distance-based gameplay checks by `lossyScale.x`.
- [ ] **Subtle Lighting**: Blend underlying textures ($70\%+$) with soft Lambert shading and modest Fresnel rims.
- [ ] **Shadow Catcher**: Include an invisible shadow catcher plane on the table for physical grounding.
- [ ] **Runtime Permissions**: Asynchronously request `Permission.Camera` at runtime before starting AR sessions.
- [ ] **Always Included Shaders**: Register dynamically loaded shaders in `GraphicsSettings.m_AlwaysIncludedShaders` to prevent build stripping.
- [ ] **Strict Visual Separation**: Keep scene files serialized with authentic project materials; swizzle AR hologram shaders only at runtime on placed content.
- [ ] **Floor & Table Surface Tracking**: Provide a dedicated plane visualizer prefab, animated placement reticle, and real-time guidance HUD before spawning AR content.
- [ ] **TrackedPoseDriver Input Bindings & Active Input Backend**: Verify `positionInput` / `rotationInput` have `<HandheldARInputDevice>` bindings (not empty arrays!) and match all tap/gesture input to `UnityEngine.InputSystem` (`Touchscreen.current`) if `activeInputHandler == 1`.
- [ ] **Scale World-Space Effects & Contact Skin**: Multiply `TrailRenderer` widths, `LineRenderer` laser widths, and `Physics.defaultContactOffset` by diorama `lossyScale.x`.
- [ ] **Preserve Hazard & Pickup Color Identity in Hologram Shaders**: Never overwrite hazards/portals/pickups with uniform cyan; preserve red/yellow/purple/green/blue semantic hues + glowing halos across both modes.

---

## 14. AR Troubleshooting Post-Mortem: Why Earlier AR Failed & What Finally Made It Work

Even after building the initial Dual-Mode rig and reticle pipeline, three real-world failures occurred sequentially on Android devices before AR worked cleanly. Recording both the dead-ends and the actual root fixes prevents repeating them.

### Failure Phase 1: Solid Black Screen in AR Mode
* **Symptom**: Switching to AR Tabletop showed HUD overlays cleanly and displayed *"Move device slowly..."*, but the camera background remained completely black on phone hardware.
* **What We Initially Tried (Insufficient)**: Ensuring `ARCameraBackground` existed and setting `arCam.backgroundColor = Color(0,0,0,0)`.
* **True Root Causes & Fixes**:
  1. **Graphics API Compatibility & Missing Renderer Features**: Early project settings had Vulkan prioritized ahead of OpenGLES3 on Android while URP renderers lacked full background command-buffer support (`ARCommandBufferSupportRendererFeature` alongside `ARBackgroundRendererFeature` on both `Mobile_Renderer` and `PC_Renderer`). Switching primary Android graphics API stability to **OpenGLES3** (with both AR URP renderer features active) restored the live optical camera passthrough feed immediately.
  2. **Rig Initialization Sequencing**: Enabling `XR Origin` before `XRGeneralSettings.Manager.InitializeLoader()` and `ARSession` had completed availability checks caused the optical camera subsystem to stall. Sequencing **Runtime Permission → `XR Loader` / `StartSubsystems()` → `ARSession` Ready/Initializing Wait → `XR Origin` / `AR Camera` / `ARPlaneManager`** made camera startup deterministic across repeated mode toggles.

### Failure Phase 2: "6 Surface(s) Found", Yet Screen Blank / No Visual Planes / Tapping Does Nothing
* **Symptom**: With the live camera feed working, the guidance HUD reported *"6 surface(s) found"*, proving ARCore had detected real planes—**yet nothing rendered over the table and tapping anywhere refused to place the maze**.
* **Two Hidden Bugs Fixed Simultaneously**:
  1. **Unbound `TrackedPoseDriver` (Silent Pose Freeze)**:
     - Inspecting `AR Camera` in `PuzzleSolver.unity` revealed its `UnityEngine.InputSystem.XR.TrackedPoseDriver` component had **empty** `m_PositionInput.m_Action.m_Bindings: []` and `m_RotationInput.m_Action.m_Bindings: []`!
     - **Effect**: ARCore tracked planes in 3D space, *but the Unity camera never moved or rotated with the phone* (`AR Camera` stayed permanently locked at `(0,0,0)` looking forward). Physical tables detected below the camera were completely outside the frozen frustum, so no plane mesh appeared on screen and screen-center raycasts missed every plane!
     - **Fix**: Explicitly wired `<HandheldARInputDevice>/devicePosition` & `deviceRotation` (and XR HMD fallbacks) in [`PuzzleSolver.unity`](Assets/Scenes/PuzzleSolver.unity) AND added a runtime self-healing fallback `DualModeManager.EnsureTrackedPoseDriverConfigured()`.
  2. **Legacy `Input` Calls Under New Input System (`activeInputHandler: 1`)**:
     - The project has `activeInputHandler: 1` (New Input System ONLY).
     - `ARPlacementManager.cs` was polling `UnityEngine.Input.touchCount` and `Input.GetMouseButtonDown(0)`, which compile to no-ops when legacy Input Manager is disabled!
     - **Fix**: Migrated single-tap placement and two-finger pinch/twist gestures to `UnityEngine.InputSystem` (`Touchscreen.current` & `Mouse.current`), added fallback raycasting at exact `tapPos` (`PlaneWithinPolygon → PlaneWithinBounds → PlaneEstimated`), and filtered UI hits cleanly via `EventSystem.RaycastAll`.

### Failure Phase 3: Cluttered Cyan Grids, Mode Reset & Dual UI Separation
* **What Worked**:
  - Replaced bright cyan grid planes with soft light-grey translucent dots (`AR_PlaneGrid.shader`, alpha `0.36`, configurable via Settings → `DEBUG` Plane Visuals toggle that drops material alpha to `0` cleanly without disabling raycast tracking).
  - Added `ARPlacementManager.ClearAllPlanes()` + conditional `ARSession.Reset()` when toggling 3D ↔ AR so even the exact same table rescans fresh on re-entry.
  - Separated combined HUD toggles into dedicated controls (`MODE: HOLD/TAP TO MOVE`, `VIEW: AR/3D`, draggable/resizable controls layout, and red `STOP` button directly above `JUMP`).

---

## 15. AR World-Space Scale Traps (TrailRenderer, Physics Contact Offset, Jump Feel & Lasers)

Once AR placement worked cleanly on tabletops ($S = 0.025$), real device testing exposed three world-space scale bugs that only manifest at $1:40$ miniaturization:

1. **Out-of-Shape `TrailRenderer` Ribbon (`CubeController.cs`)**:
   - Unity's `TrailRenderer` evaluates `startWidth`, `widthMultiplier`, and `minVertexDistance` in **world meters**, NOT parent scale. At $S = 0.025$, `startWidth = 0.7m` rendered a **28× wider giant triangle tail** across the 2.5cm cube.
   - **Fix**: Store baseline `baseTrailWidthMultiplier` and `baseTrailMinVertexDist` in `Awake()`, and scale them continuously in `SyncScaleDependentEffects(s)` (`widthMultiplier = base * s`, `minVertexDistance = max(0.001f, base * s)`), plus `trail.Clear()` on scale discontinuities and `jumpFx` `ParticleSystemScalingMode.Hierarchy`.

2. **AR Jump Snagging & Low Hang-Time (`Physics.defaultContactOffset` + AR-Only Jump Tuning)**:
   - Unity's global `Physics.defaultContactOffset = 0.01m` (`1cm` skin) is negligible for a `1m` cube ($1\%$), but for a **`2.5cm` AR cube**, `1cm` on both the cube and adjacent walls/floors creates a huge **$80\%$ invisible collision envelope** that snags jumping cubes against neighboring walls and floor manifolds. Combined with tiny world-space airtime (~$0.22\text{ s}$), jumps felt muted and top-clipped.
   - **Fix (AR Mode Only)**: Dynamically scale `Physics.defaultContactOffset` and the player `Collider.contactOffset` proportional to placement scale (`Mathf.Clamp(0.01f * s, 0.001f, 0.01f)` in AR, `0.01f` restored in 3D), and apply dedicated AR-only jump tuning (`arJumpVelocityMultiplier = 1.32f`, `arFallGravityMultiplier = 1.15f`) without affecting 3D Normal physics.

3. **Unscaled Laser Beams & Lava Overlap Boxes (`LaserEmitter.cs`, `SweepLaser.cs`, `LavaField.cs`)**:
   - Hardcoded `LineRenderer` beam length/widths (`beamLength = 7.2f`, `startWidth = 0.38f`) and unrotated `beamDirection` ignored parent `0.025x` scale and diorama twist rotation; `LavaField`'s `OverlapBoxNonAlloc` used unscaled `0.62f` half-extents (`25` AR tiles wide!).
   - **Fix**: Scaled beam length, width, damage radius, and overlap extents by `lossyScale.x` and transformed beam directions through `transform.TransformDirection(...)`.

---

## 16. Category-Aware AR Holograms & GPU-Instanced Visual Halos

### The Problem
`ARHologramManager.ApplyHologramToRenderer()` originally tinted untextured meshes cyan (`holoBaseColor`, `rimGlowColor`), and textured materials like `L4_Lava` (`hasTex == true`) bypassed color preservation completely—washing **lava walls, lasers, turret projectiles, teleporter pairs, shields, and heart sprites** into look-alike cyan/dark shapes in AR.

### How We Solved It
1. **Semantic AR Hologram Overrides + HDR Bloom Channel (`AR_HologramOverlay.shader` + `ARHologramManager.cs`)**:
   - Added optional `[HDR] _EmissionColor` (default `(0,0,0,0)` so normal cyan walls/floors stay identical and subtle).
   - Categorized materials & components into semantic palettes in both 3D & AR:
     - **Lava Walls (`LavaWall` / `L4_Lava`)**: Molten crimson **Red** + red HDR glow.
     - **Lasers (`LaserEmitter`, `SweepLaser`, `L5_Laser`)**: Crisp vivid **Red** core + warning amber.
     - **Laser Balls (`Turret`, `Projectile`, `L3_Projectile`, `L5_Bolt`)**: Bright electric **Yellow** core + yellow glow.
     - **Teleporter Portals (`Teleporter`)**: Distinct **Purple** (`L4_TeleA`) and **Green** (`L4_TeleB`) preserved cleanly with matching particle bursts.
     - **Shield (`ShieldOrb`, `L4_Shield`) & Hearts (`HeartPickup`)**: **Blue** shield glow and **Crimson-Red** heart glow.
2. **GPU-Instanced Analytical Halo Shader (`Custom/HaloGlow.shader`)**:
   - Built a single lightweight URP additive shader (`Blend SrcAlpha One`, `ZWrite Off`, `#pragma multi_compile_instancing`) supporting **Sphere aura**, **Portal Ring Disc**, **Soft Box Rim**, and **Laser Gaussian beam sheath**, excluded `"HaloFX"` objects from cyan hologram replacement, and registered `"Custom/HaloGlow"` in `GraphicsSettings.m_AlwaysIncludedShaders` so Android builds retain every halo cleanly. Using soft volumetric **Sphere auras** on `LavaWall` clusters eliminated inter-block rectangular seam artifacts across contiguous lava corridors.

---

## 17. Level 4 Death Loop, Hold-to-Move Input Drift, Surface Detection Cap & Mobile Optimization

### 1. Why Level 4 Kept Killing the Cube Repeatedly
* **Symptom**: On Level 4 (especially in AR Tabletop), the player cube repeatedly lost hearts and died within seconds even away from visible hazards or shortly after respawning.
* **Root Causes**:
  1. **Unscaled `Patroller` Hitboxes (`Patroller.cs`)**: Level 4 contains **7 patrolling hazards** (`Patroller_0`..`Patroller_6`). Each patroller checked overlap against the player using raw world coordinates (`Mathf.Abs(d.x) < hitHalfSize.x` where `hitHalfSize = (1.1, 1.1, 1.1)`). When `ARContentRoot` was miniaturized to $S = 0.025$ (or $0.020$), an unscaled $1.1\text{ m}$ half-extent projected into a **$44\text{--}55$ diorama-unit half-width ($88\text{--}110\text{ m}$ death zone per patroller!)**—blanketing almost the entire $112.5\text{ m} \times 112.5\text{ m}$ Level 4 grid across walls and corridors. Furthermore, checking world X/Z offsets without `Quaternion.Inverse(transform.rotation)` broke alignment whenever the AR diorama was rotated.
  2. **World-Space & Unscaled Level Components (`CrumblePlatform.cs`, `JumpingPlatform.cs`, `PressurePlate.cs`, `Crate.cs`)**: Platforms and puzzle triggers cached world-space `transform.position` in `Awake()` and ran `Physics.OverlapBoxNonAlloc` with unscaled extents (`1.0m`–`1.3m` world probes acting like `50m` probes in AR), causing platforms to warp back to unscaled coordinates and plates/crates to trigger across rooms.
  3. **Pre-Placement Simulation Frames (`DualModeManager.cs`)**: Entering AR mode waited on an asynchronous coroutine (`SetupARTabletopModeRoutine`) while `ARContentRoot` remained active for initial frames before `Reposition()` hid it.
* **Fixes Applied**:
  - Transformed `Patroller` player-offset checks into local space (`Quaternion.Inverse(transform.rotation) * delta`) and scaled `hitHalfSize` by parent diorama scale (`transform.parent.lossyScale.x`).
  - Converted `CrumblePlatform`, `JumpingPlatform`, and `Crate` to `localPosition` / `localRotation` and scaled all `OverlapBoxNonAlloc` probes on `CrumblePlatform`, `PressurePlate`, and `Crate` by `lossyScale.x`.
  - Deactivated `ARContentRoot` synchronously inside `DualModeManager.SetMode(GameMode.ARTabletop)` before starting the AR initialization coroutine.

### 2. Why Hold-to-Move Restricted & Stopped Working Across All Levels
* **Root Causes**:
  1. **Touch `OnPointerExit` Sensitivity (`UIHoldButton.cs`)**: D-pad arrows are `94×94` UI elements. When holding a direction on a mobile touchscreen, tiny thumb micro-slides outside the rect fired `IPointerExitHandler` and killed movement even though the finger remained pressed on screen.
  2. **Additive Ref-Counting Desync (`UIHoldButton.cs` + `CubeController.cs`)**: `OnPointerDown` added `+direction` (`heldInput += d`) and `Release()` subtracted `-direction` (`heldInput -= d`). Whenever `StopMovement()`, `SetFrozen(true)`, `TeleportTo()` (teleporters/voids/respawns), or pause ran while holding a button, `CubeController` zeroed `heldInput = Vector2.zero` while `UIHoldButton.held` remained `true`. Releasing the finger afterward added `-direction` to `0`, leaving a **permanent negative ghost offset** that cancelled future forward holds across levels.
  3. **Steep Top-Down Camera Pitch Snap (`CubeController.cs`)**: Looking straight down at a tabletop diorama drove `referenceCamera.forward.xz` below `0.001`, snapping `camFwd` to world `+Z` unpredictably.
* **Fixes Applied**:
  - Replaced additive counting with idempotent set tracking (`HashSet<UIHoldButton>` in `CubeController.SetButtonHeld`), added `ClearAllHeldInputs()` synchronization back to buttons, removed `IPointerExitHandler` in favor of `IPointerUpHandler` + `ICancelHandler` + `OnDisable` + `OnApplicationFocus(false)` (UGUI keeps `pointerPress` locked until release), and fell back to `referenceCamera.up` projected onto XZ during steep downward camera angles.

### 3. Surface Detection Cap (3–5 Planes), Post-Placement Stop, Persistence & Optimization
* **AR Surface Cap & Shutdown (`ARPlacementManager.cs`)**:
  - Subscribed to `ARPlaneManager.trackablesChanged` and capped active scanning at `maxDetectedPlanes = 4` (within 3–5). Once `allowedPlaneIds.Count >= maxDetectedPlanes`, `requestedDetectionMode` is set to `PlaneDetectionMode.None` while keeping `planeManager.enabled = true` so `ARRaycastManager` remains registered in `FallbackRaycastRegistry` for tap placement.
  - Once `PlaceContent()` places the diorama, `StopPlaneDetection()` sets `requestedDetectionMode = None` **before** `planeManager.enabled = false`, disables `ARRaycastManager`, and hides all planes. Surface detection stays off across level progression and respawns unless `Reposition`, `Retry`, `Restart`, or `Mode/Level Switch` is tapped.
* **Diorama Scale & Pinch Zoom (`ARPlacementManager.cs`)**: Reduced `defaultScale` from `0.025f` to `0.020f` (`minScale = 0.008f`, `maxScale = 0.060f`), excluded UI touches from triggering accidental pinch/twist gestures while using D-pad + Jump, and synced physics contact offsets + `TrailRenderer` widths dynamically during zoom.
* **Movement Style Persistence & Haptics Cancellation**: Persisted `CubeController.PreferredStickyMode` in `PlayerPrefs` (`MR_MoveStickyMode`) with an `OnStickyModeChanged` event so both active and inactive scenes stay locked to the player's Hold vs Tap choice; added `HapticsManager.CancelAll()` + `SuppressFor()` and `GameAudioManager.StopAllSfx()` on mode/pause/menu switches.
* **Performance Optimizations**: Eliminated `SpikeTrap.SetPad()`'s `50 Hz` `.material` instance allocation leak using `MaterialPropertyBlock` (also applied to `Checkpoint`, `PhasePlatform`, `PressurePlate`), added `#pragma multi_compile_instancing` to `AR_HologramOverlay.shader`, removed redundant collider creation/destruction per `Turret` shot, cached `PointerEventData` & guidance strings, and set `Application.targetFrameRate = 60` (`vSyncCount = 0`).

---

## 18. Post-Polish Physics & Controls Rework: AR Jump Parabolic Arc, Map-Cube Transform Sync & Robust Hold Movement

Following visual HD UI polish, user testing with video footage (`problem.mp4`) revealed three grave gameplay and physics bugs in AR Tabletop mode that prevented smooth, enjoyable gameplay.

### 1. The AR Jumping "Drop Like a Brick" Defect (The Gravity-Scale Mismatch)
* **Video Evidence (`problem.mp4`, 0:10–0:17)**:
  On Level 3, the player pressed `JUMP` to cross a gap over pit/crumble platforms. The cube performed jump visuals and SFX, but snapped back down in $\sim 0.2\text{ s}$, crossing barely $1.5$ maze units before falling into the void and triggering *"YOU DIED"*.
* **Mathematical Root Cause**:
  In `CubeController.cs`:
  $$v_{\text{horiz}} = \text{moveSpeed} \cdot s = 6 \cdot 0.02 = 0.12\text{ m/s}$$
  $$v_{\text{jump}} = \text{jumpSpeed} \cdot \sqrt{s} \cdot \text{arJumpMult} = 7 \cdot \sqrt{0.02} \cdot 1.32 \approx 1.306\text{ m/s}$$
  $$\mathbf{g = \text{Physics.gravity.y} = -9.81\text{ m/s}^2 \quad (\text{UNSCALED})}$$
  
  Scaling jump velocity by $\sqrt{s}$ while leaving gravity unscaled kept peak height proportional to diorama scale, **but compressed total airtime $t_{\text{air}} = \frac{2 v}{g}$ by $\sqrt{s}$ ($7.07\times$ shorter airtime!)**:
  - In 3D Normal mode ($s = 1.0$): Airtime was $\sim 1.30\text{ s}$, horizontal jump distance was $\sim 7.8\text{ maze units}$ (easily clearing 4m–6m gaps).
  - In AR mode ($s = 0.020$): Airtime was **only $0.25\text{ s}$**, and horizontal jump span was **only $1.5\text{ maze units}$**!
* **The Scale-Invariant Physics Solution**:
  Scale **both** jump velocity ($v_{\text{jump}} = \text{jumpSpeed} \cdot s \cdot \text{mult}$) AND effective gravity ($g_{\text{eff}} = g \cdot s \cdot \text{fallMult}$) **linearly** by $s$:
  $$t_{\text{air}} = \frac{2 (v_0 \cdot s)}{g_0 \cdot s} = \frac{2 v_0}{g_0} \quad (\mathbf{\text{CONSTANT AT ANY SCALE!}})$$
  $$h_{\text{peak}} = \frac{(v_0 \cdot s)^2}{2 (g_0 \cdot s)} = s \cdot \frac{v_0^2}{2 g_0} \quad (\mathbf{\text{SCALES EXACTLY WITH DIORAMA!}})$$
  - Set `rb.useGravity = false` on `PlayerCube` and dynamic `Crate`s, applying custom scale-scaled gravity in `FixedUpdate()`:
    `v.y += Physics.gravity.y * s * gravScale * Time.fixedDeltaTime`.
  - Inset `PlayerCube`'s `BoxCollider.size` slightly to `(0.94, 1.0, 0.94)` so floor seams and corner edges never snag jump takeoffs.
  - Verified across scales ($s = 1.0$, $s = 0.020$, $s = 0.008$): Airtime is consistently $1.30\text{--}1.40\text{ s}$, peak clearance is $\sim 2.8\text{ local maze units}$ (clearing walls/obstacles comfortably), and horizontal span is $\sim 8.4\text{ units}$.

### 2. AR Map vs Cube Desync & Wall Collisions on Map Move/Rotate/Pinch
* **Symptom**: When the tabletop maze was placed, any subsequent position shift, two-finger twist-rotation, or pinch-scale moved the maze diorama, but `PlayerCube` lagged behind in world space, colliding with walls and wedging into corridors.
* **Root Causes**:
  1. `PlayerCube` uses a non-kinematic `Rigidbody` with `Interpolate`. When `ARPlacementManager` altered `contentRoot.position`, `rotation`, or `localScale` during `Update()`, PhysX maintained `rb.position` and `rb.linearVelocity` in stale world coordinates.
  2. `rb.constraints = RigidbodyConstraints.FreezeRotation` kept the cube's `BoxCollider` locked to world `Quaternion.identity`. When the player twisted the AR diorama $45^\circ$, the maze corridors rotated while the cube collider stayed unrotated, wedging its diagonal corners firmly against narrow corridor walls.
* **Fixes Applied**:
  - In `ARPlacementManager.cs`: Added `CaptureDynamicRigidbodies()` and `RestoreDynamicRigidbodies()`. Before scaling or rotating `contentRoot`, all dynamic child rigidbodies snapshot their diorama-local position and local velocity (`contentRoot.InverseTransformPoint` / `InverseTransformVector`). After transforming the map and synchronizing physics (`Physics.SyncTransforms()`), world positions, velocities, and rotations are restored cleanly with temporary `rb.interpolation = None`.
  - In `CubeController.cs`: Added `SyncWithParentTransformIfMoved()` to continuously track parent matrix changes, keeping `rb.rotation = transform.parent.rotation` so the cube's collision box always faces corridor walls squarely.

### 3. "Hold to Move" Mid-Corridor Stops & Movement Rework
* **Symptom**: While holding a D-pad button in `HOLD TO MOVE` mode, the cube would stop or stall mid-way down a corridor.
* **Root Causes**:
  1. **Nadir Camera Discontinuity**: In tabletop AR, the camera looks steeply down at the table ($\sim 75^\circ\text{--}88^\circ$). Small handheld tilts caused `referenceCamera.forward` projected onto XZ to cross zero, flipping the camera's forward direction by $90^\circ\text{--}180^\circ$ and driving the cube diagonally into side walls.
  2. **Multi-Touch & EventSystem Interruptions**: When holding a D-pad button with the left thumb and tapping `JUMP` with the right thumb, UGUI's default `Selectable` navigation emitted `OnCancel` or shifted focus, releasing the held D-pad button.
  3. **Touch Border Slip**: Thumb micro-movements on `94×94` D-pad buttons caused touch tracking to drop.
* **Fixes Applied**:
  - **Cardinal Corridor Snapping**: In `CubeController.ComputeMoveDirection()`, single-axis D-pad input is snapped directly onto the nearest cardinal axis of the maze (`transform.parent.right` / `forward`) with directional hysteresis. Diagonal wall-scraping is completely eliminated, allowing smooth gliding down every corridor.
  - **Smooth Nadir Camera Basis**: Blended `camFwd = referenceCamera.forward + referenceCamera.up * 0.35f` before horizontal projection, eliminating any abrupt flip at steep overhead camera angles.
  - **Active Pointer Lock (`UIHoldButton.cs`)**: Switched to pointer-ID tracking using `ExtendedPointerEventData` (`Touchscreen.current.touches`). In `Update()`, held state checks if the tracked touch is still in progress, ignoring spurious `OnCancel` events caused by multi-touch taps on `JUMP`.
  - **Touch Raycast Padding**: Set `img.raycastPadding = new Vector4(-8, -8, -8, -8)` on D-pad buttons for a generous, forgiving touch target that doesn't drop when thumbs slide slightly.
  - Set `Navigation.Mode.None` and `EventSystem.sendNavigationEvents = false` on HUD controls to avoid automatic navigation focus theft.

---

## 19. Mobile Release & Multi-Mode Polish: Life Loss Prevention, Persistent Core State, AR Hologram Yellow Crate & Mobile UI Cleanliness

Following community testing across both 3D Normal and AR Tabletop modes, five specific multi-mode bugs and UI inconsistencies were identified and resolved.

### 1. UI Irregularity & Scanning State Clutter Across Modes
* **Root Causes**:
  - In AR Tabletop mode, when scanning for a surface (`!isPlaced`), the full gameplay HUD (D-Pad cluster, Jump button, Stop button, Hearts, Level text, Reposition) remained active and rendered on screen over the live camera feed. This not only cluttered the camera view, but thumb touches on the screen were intercepted by UGUI raycasts (`IsPointerOverUI`), blocking diorama placement taps.
  - When switching between 3D Normal and AR Tabletop modes, HUD elements shifted or flashed inconsistently because placement state wasn't tied to HUD visibility.
* **Fixes Applied**:
  - Added `ARPlacementManager.OnPlacementStateChanged(bool placed)` event.
  - In `GameUI.cs`, bound HUD visibility to placement state: while scanning in AR (`!isPlaced`), gameplay touch controls are automatically hidden, leaving an unobstructed camera feed with the AR reticle and guidance text. Once the user taps to place the diorama (`isPlaced = true`), gameplay controls seamlessly appear.
  - When `REPOSITION` is clicked, gameplay controls hide immediately while scanning a new surface and re-appear upon tap. In 3D Normal mode, the HUD remains fully active while `REPOSITION` is hidden.

### 2. Player Cube Losing Life on Mode Switch (3D $\leftrightarrow$ AR)
* **Root Causes**:
  - In Levels 3–5, `fallsCostHealth == true`. When switching between 3D Normal and AR Tabletop modes, `activeLevelAnchor.localPosition` jumps (e.g., $(0, 0, -200)$ for Level 3, $(0, 0, -400)$ for Level 4) while `arContentRoot.localScale` scales from $1.0$ to $0.020$.
  - Because `PlayerCube` is parented under `ActiveLevelAnchor`, during transition frames before PhysX synchronized the Rigidbody's position, `localPosY = transform.parent.InverseTransformPoint(rb.position).y` briefly evaluated to less than `fallY` ($-9\text{ m}$).
  - This immediately triggered `GameAudioManager.PlayFallVoid()`, `HapticsManager.HeavyHit()`, and `health.ForceDamage(1)`, deducting a heart every time the player toggled the mode switch button.
* **Fixes Applied**:
  - Added `SuppressDamage(float seconds)` to `PlayerHealth.cs`, guaranteeing that all incoming damage (including `ForceDamage`) is completely suppressed during mode transitions.
  - Added `modeSwitchGraceTimer` and `SuppressFallDamage(float duration = 2.5f)` to `CubeController.cs`, preventing void-fall checks while parent matrices are re-anchoring.
  - In `DualModeManager.SetMode()`, `SetupNormal3DMode()`, `ARPlacementManager.PlaceContent()`, and `Reposition()`, granted a 2.5s fall damage immunity window and called `Physics.SyncTransforms()` before re-enabling gameplay physics.

### 3. Level 3 Yellow Cube (Puzzle Crate) Missing in AR Mode
* **Root Causes**:
  - In `ARHologramManager.cs`, `ResolveCategoryAppearance()` had category overrides for lava, lasers, projectiles, teleporters, and shields, but **no override** for `L3_Crate` or `Crate`.
  - `L3_Crate` has no diffuse texture (`_BaseMap == null`). Consequently, it fell into the default structural geometry case, turning into a semi-transparent frosted cyan cube (`holoBaseColor`, alpha $0.76$, emission $0$). On tabletop surfaces, it blended completely into the cyan floor and walls, appearing completely missing/invisible to the player.
  - In addition, at diorama scale $s = 0.020$, the crate's `BoxCollider.contactOffset` ($0.01\text{ m}$) was one third of the crate's entire thickness ($0.03\text{ m}$), causing potential edge-snagging against floor tiles.
* **Fixes Applied**:
  - In `ARHologramManager.cs`, added a dedicated visual category for `L3_Crate` / `Crate`:
    - Base Color: Vibrant Golden Yellow (`new Color(0.98f, 0.78f, 0.12f, 0.96f)`)
    - Rim Color: Warm Amber Glow (`new Color(1.0f, 0.92f, 0.32f, 1.0f)`)
    - Emission: Glowing Amber (`new Color(1.8f, 1.35f, 0.16f, 1.0f)`)
    - High Opacity: $0.96$
  - In `Crate.cs` and `DualModeManager.SyncPhysicsContactOffset()`, dynamically scaled `contactOffset` for crates to match `Physics.defaultContactOffset`, ensuring the crate slides smoothly across pressure plates without sinking.

### 4. Level 4 Energy Core Count Resetting on Mode Switch
* **Root Causes**:
  - During mode switches, `DualModeManager` called `arContentRoot.gameObject.SetActive(false)` / `SetActive(true)` to hide the diorama while scanning.
  - In `CoreGate.cs`, `OnEnable()` unconditionally executed:
    `Collected = 0; if (gate != null) gate.Activate(false); Refresh(true);`
  - In `EnergyCore.cs`, `OnEnable()` unconditionally executed:
    `taken = false; if (visual != null) visual.gameObject.SetActive(true);`
  - Deactivating and reactivating `arContentRoot` re-enabled all child GameObjects under `Level4`, wiping collected cores back to 0 and locking the gate even if the player had already collected 1, 2, or 3 cores!
* **Fixes Applied**:
  - Decoupled casual `OnEnable` from core collection state reset.
  - In `CoreGate.cs`: `OnEnable()` preserves `Collected` and restores the open gate state if `Collected >= total`. Added `ResetGate()` for explicit restarts.
  - In `EnergyCore.cs`: `OnEnable()` maintains `taken` state, only activating visual geometry if `!taken`. Added `ResetCore()` for explicit restarts.
  - In `LevelManager.EnterLevel(4)`: Explicitly invoke `ResetCore()` and `ResetGate()` ONLY when starting or retrying Level 4 from scratch. Across mode toggles and AR repositioning, all collected cores stay safely collected.

### 5. Main Menu Mobile Polish & Official Game Logo
* **Fixes Applied**:
  - **Removed Desktop Keyboard Prompts**: In `PuzzleSolver.unity`, deactivated the bottom `Hint` GameObject containing `MOVE D-PAD / WASD • JUMP SPACE • PAUSE ESC` and cleared its text string, ensuring a clean mobile presentation.
  - **Updated Game Title**: Updated Main Menu `[Title]` text from `STARBOUND CUBE` to **`STARBOUND : MAZE RUNNER`** with balanced font sizing (`76 pt`) and centered alignment.
  - **Official Game Logo & App Icon**: Configured `Assets/UI/HD/Starbound_Emblem_HD.png` as the default global application icon in `ProjectSettings.asset` under `m_BuildTargetIcons`, matching the mobile product branding.

---

## 20. AR Visual & Shader Refinement: Scale-Adaptive Hologram Scan, Particle Hierarchy Scaling, Mobile On-Chip MSAA & High-Contrast Alpha Reticle

Following QA evaluation of visual polish in AR Tabletop mode, four specific rendering, shader, and anti-aliasing issues were identified and resolved to ensure high visual fidelity on physical mobile hardware.

### 1. The Wavelength-Scale Mismatch in Holographic Scan Lines (`AR_HologramOverlay.shader`)
* **Root Cause**:
  In `AR_HologramOverlay.shader`, the moving holographic scan-line effect was calculated using world-space Z coordinates:
  $$\text{scan} = \sin(z_{\text{world}} \cdot \omega_{\text{scan}} - t \cdot v_{\text{speed}})$$
  With the default spatial frequency $\omega_{\text{scan}} = 0.08\text{ rad/m}$, the physical spatial wavelength was:
  $$\lambda = \frac{2\pi}{\omega_{\text{scan}}} = \frac{2\pi}{0.08} \approx 78.54\text{ meters}$$
  In standard 3D mode ($40\text{m}$ maze span), this wavelength produced smooth sweeping beams. But in AR Tabletop mode at $s = 0.020$, the entire maze diorama spans only $40\text{m} \times 0.020 = 0.8\text{ meters}$.
  Across an $0.8\text{m}$ diorama, $\Delta\phi = 0.8 \times 0.08 = 0.064\text{ radians}$ (less than $3.7^\circ$ of a sine period). Consequently, instead of visible traveling scan bars gliding across corridors, the entire diorama pulsed in uniform brightness all at once.
* **Fix Applied**:
  - Dynamically scale scan frequency $\omega_{\text{scan}}$ inversely with diorama scale in `ARHologramManager.cs`:
    $$\omega_{\text{eff}} = \frac{0.09}{s} \quad \implies \text{at } s = 0.020, \quad \omega_{\text{eff}} = 4.5\text{ rad/m} \quad (\lambda \approx 1.4\text{m})$$
  - In `AR_HologramOverlay.shader`, added a diagonal spatial projection:
    $$\text{scan} = \sin((z_{\text{world}} + 0.25 \cdot x_{\text{world}}) \cdot \omega_{\text{scan}} - t \cdot v_{\text{speed}})$$
    This guarantees that even when the user rotates the diorama $90^\circ$ on their tabletop, scan lines continue to glide continuously across the maze rather than stalling parallel to the corridor walls.
  - Hooked `ARPlacementManager.ApplyDioramaScale()` to `ARHologramManager.UpdateHologramScale()` so two-finger pinch-to-zoom keeps scan line density perfectly proportioned in real time.

### 2. Miniature Particle Hierarchy Scaling (`CrumblePlatform`, `LaserEmitter`, `Level5Bridge`)
* **Root Cause**:
  By default, Unity `ParticleSystem` components initialize with `main.scalingMode = ParticleSystemScalingMode.Shape`. In AR mode, when parent objects are miniaturized by $50\times$ ($s = 0.020$), `Shape` mode continues emitting particles and debris chunks at unscaled 1:1 real-world sizes (e.g., $1.0\text{m}$ stone slabs and massive laser sparks flying several meters across the room).
* **Fix Applied**:
  - In `CrumblePlatform.cs`, `LaserEmitter.cs`, and `Level5Bridge.cs`, explicitly enforced:
    `main.scalingMode = ParticleSystemScalingMode.Hierarchy;`
    in `Awake()`.
  - Particle sizes, emission shapes, and velocities now scale proportionally with parent transforms, ensuring debris and muzzle flashes stay miniature and grounded inside the tabletop diorama.

### 3. Mobile Anti-Aliasing (MSAA 4x) on Tile-Based Deferred GPUs (`Mobile_RPAsset` & `QualitySettings`)
* **The Problem**:
  `ProjectSettings/QualitySettings.asset` and `Assets/Settings/Mobile_RPAsset.asset` had anti-aliasing disabled (`antiAliasing: 0`, `m_MSAA: 1`).
  In AR Tabletop mode, thin holographic glass walls and cyan geometric borders are viewed against real-world camera video feeds with high geometric contrast. Without anti-aliasing, camera movement produced distracting temporal aliasing ("staircase crawling" and pixel sparkling).
* **GPU Architectural Analysis & The On-Chip Tile Cache Advantage**:
  - On desktop GPUs with discrete VRAM (Immediate-Mode Rendering), 4x MSAA requires allocating $4\times$ color and depth memory in VRAM and resolves full-screen resolve passes across the memory bus, consuming considerable memory bandwidth.
  - On **Mobile GPUs** (Qualcomm Adreno, ARM Mali, Apple Silicon, PowerVR), rendering is **Tile-Based Deferred (TBDR)**. Mobile GPUs divide the screen into small $16\times 16$ or $32\times 32$ pixel tiles that reside inside ultra-fast **on-chip SRAM / GMEM tile cache** ($>1\text{ TB/s}$ local bandwidth).
  - During rendering, the multi-sample coverage test and sub-sample color evaluation happen **entirely within on-chip tile memory**.
  - When the tile finishes rendering, the GPU hardware automatically resolves the 4 sub-samples down to a single final anti-aliased pixel **before writing to external LPDDR DRAM**.
  - As a result, **4x MSAA on mobile tile-based GPUs incurs virtually zero external memory bandwidth penalty** while dramatically eliminating geometric edge aliasing.
* **Fix Applied**:
  - Updated `Assets/Settings/Mobile_RPAsset.asset` to `m_MSAA: 4`.
  - Updated `ProjectSettings/QualitySettings.asset` to `antiAliasing: 4` across all mobile and performant quality tiers.
  - Hologram edges, maze walls, and laser beams now render with smooth, crisp vector-like edges against physical tabletops.

### 4. AR Reticle Contrast on Bright Surfaces (`AR_Reticle.shader`)
* **Root Cause**:
  `AR_Reticle.shader` used additive blending:
  `Blend SrcAlpha One`
  Additive blending adds pixel color directly to the framebuffer:
  $$C_{\text{final}} = C_{\text{src}} \cdot \alpha + C_{\text{dst}}$$
  When the device camera was pointed at a white tablecloth, white marble floor, or sunlit surface ($C_{\text{dst}} \approx (1, 1, 1)$), saturated addition clamped to white:
  $$C_{\text{final}} = \min(1.0, \; C_{\text{src}} + 1.0) = (1.0, 1.0, 1.0)$$
  The placement reticle became completely invisible on white or bright surfaces, leaving players unable to aim and place the diorama.
* **Fix Applied**:
  - Switched shader blend mode to standard transparency:
    `Blend SrcAlpha OneMinusSrcAlpha`
  - In `frag()`, rendered a high-contrast dark slate-blue shadow ring ($2.6\times$ thickness) behind the cyan ring, center dot, and 4 corner alignment ticks.
  - On bright white tables, the dark outline provides sharp, immediate contrast that makes the cyan reticle pop; on dark carpets or floors, the vibrant cyan and teal emission remains punchy and distinct.

---

## 21. Gameplay, Hazard & Platform Physics Polish: Hitbox Local Space, Zero-Alloc Contacts, Local Spinner Rotation & Moving Platform Momentum

Following a comprehensive codebase and level audit across physics interactions, environmental hazards, moving platforms, and collectible systems, five discrete edge cases were resolved to ensure seamless gameplay continuity, deterministic collision resolution, and frame-rate stability across both Standard 3D and AR Tabletop modes.

### 1. AR Holographic Category Classification for Energy Cores (`ARHologramManager.cs`)
* **The Problem**:
  In levels 3 and 4, Energy Cores serve as mandatory puzzle collectibles required to open the exit beacon. While visually vibrant in 3D mode, the AR Tabletop procedural shader system (`AR_HologramOverlay.shader`) replaces all scene materials with holographic projections. Because `EnergyCore` objects had not been assigned a specific category ID in `ARHologramManager.cs`, they defaulted to Category 0 (General Environment / Level Geometry). As a result, cores rendered as dull, dark blue-gray meshes with zero bloom emission, causing them to blend invisibly into maze floors and wall geometry on physical tables.
* **Fix Applied**:
  - Registered Category 9 specifically for `EnergyCore` objects in `ARHologramManager.cs`.
  - Configured high-intensity electric cyan emission tint ($\text{RGB} = (0.05, 0.95, 1.0)$) with elevated fresnel rim strength and active scan pulsation.
  - Cores now radiate with distinct visual pop in AR mode, immediately readable against room lighting and physical table textures.

### 2. Rotation-Dependent Hitbox Distortion in Spike Traps (`SpikeTrap.cs`)
* **Root Cause**:
  `SpikeTrap.cs` evaluated player damage contact using a manual Axis-Aligned Bounding Box (AABB) proximity test directly in world space:
  $$\Delta \vec{p}_{\text{world}} = \vec{p}_{\text{player}} - \vec{p}_{\text{trap}}$$
  $$|\Delta p_x| \le \text{halfExtents.x} \quad \land \quad |\Delta p_z| \le \text{halfExtents.z}$$
  This computation implicitly presumed the trap orientation was aligned with the global Cartesian axes ($\theta = 0^\circ$). When a rectangular spike trap plate (e.g. $1.5\text{m} \times 3.0\text{m}$) was rotated by $90^\circ$ to fit a north-south corridor, the world X and Z dimensions remained fixed to the unrotated extents. Furthermore, when the user rotated the entire maze diorama on their physical table in AR, the world AABB became completely disconnected from the visual mesh, producing severe dead zones (standing directly on spikes without taking damage) and phantom trigger zones (taking damage while standing on safe adjacent tiles).
* **Fix Applied**:
  - Transformed the displacement vector from world space into the trap plate's local coordinate system:
    $$\Delta \vec{p}_{\text{local}} = \mathbf{R}_{\text{trap}}^{-1} (\vec{p}_{\text{player}} - \vec{p}_{\text{trap}})$$
    ```csharp
    Vector3 d = Quaternion.Inverse(transform.rotation) * (player.position - transform.position);
    ```
  - Evaluated local coordinate offsets against local box half-extents.
  - Spike trap hitbox detection is now mathematically invariant under arbitrary rotations, corridor orientations, and AR tabletop placements.

### 3. Wobble and Plane Clipping in Hazard Spinners (`Patroller.cs`)
* **Root Cause**:
  Rotating hazard obstacles (spinning blade turnstiles and obstacle beams) updated continuous rotation using:
  ```csharp
  transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
  ```
  `Space.World` forces rotation around the global vertical axis $(0, 1, 0)^T$. When a hazard was mounted onto an inclined ramp, placed on a tilted bridge, or when the AR diorama was placed on a slightly angled physical surface, rotating around world up forced the spinner's plane to precess like a wobbling top. Consequently, the spinning blades dipped through the floor on one half of their cycle and hovered high above the player's head on the other half.
* **Fix Applied**:
  - Changed the rotation coordinate space from `Space.World` to `Space.Self`:
    ```csharp
    transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.Self);
    ```
  - Rotation is now strictly pinned to the hazard's local transform normal $\mathbf{u}_{\text{local}} = (0, 1, 0)$, keeping the sweep perfectly planar and flush with the floor regardless of diorama tilt or ramp inclination.

### 4. Zero-Allocation Contact Auditing & Physics Stay Fail-Safe (`MovingPlatform.cs` & `JumpingPlatform.cs`)
* **Root Cause**:
  Collision detection on moving platforms used Unity's legacy contact array accessor:
  ```csharp
  foreach (ContactPoint c in collision.contacts)
  ```
  In Unity PhysX, accessing `collision.contacts` creates a heap allocation (`new ContactPoint[n]`) on every collision frame. Across multiple moving platforms and continuous contact evaluation at $50\text{Hz}$ (`FixedUpdate`), this generated continuous garbage collection (GC) churn, precipitating recurring micro-stutters and frame drops on memory-constrained mobile devices.
  Additionally, relying solely on `OnCollisionEnter` meant that if a player landed on a platform during a physics step where `OnCollisionEnter` was dropped or masked by a multi-collider event, the player would never register as a rider and would slide off.
* **Fix Applied**:
  - Replaced `collision.contacts` with the zero-allocation API:
    ```csharp
    int count = collision.contactCount;
    for (int i = 0; i < count; i++)
    {
        var contact = collision.GetContact(i);
        if (contact.normal.y < -0.5f) { rider = player; break; }
    }
    ```
  - Added an `OnCollisionStay` fail-safe rider check: if `rider == null`, evaluate contacts and register the player, ensuring rider locking never fails even after high-velocity bounces or frame drops.
  - Exposed `CurrentVelocity` as a public property computed from local frame delta and `Time.fixedDeltaTime`, normalized for parent scale.

### 5. Moving Platform Momentum Inheritance & Mid-Air Jump Continuity (`CubeController.cs`)
* **Root Cause**:
  In `CubeController.cs`, horizontal linear velocity was completely overwritten every physics step:
  $$v_x = \text{moveDir}_x \cdot v_{\text{move}}, \quad v_z = \text{moveDir}_z \cdot v_{\text{move}}$$
  While standing on a moving platform (traveling at $2.2\text{m/s}$), the platform moved the cube's position directly via transform delta. However, the instant the player pressed Jump:
  - The vertical jump impulse was applied ($v_y = v_{\text{jump}}$), lifting the cube off the platform.
  - Contact was broken, clearing the platform rider registration.
  - Because $v_x$ and $v_z$ only accounted for input direction, jumping without holding the D-pad resulted in $v_x = 0, v_z = 0$.
  - The cube lost all horizontal momentum in mid-air, while the platform continued moving at $2.2\text{m/s}$ underneath it. The platform slid out from under the player, causing the cube to plummet straight down into the void.
* **Architectural Solution**:
  - **Sensing Platform Velocity**: In `CheckGrounded()`, when querying ground colliders via `Physics.OverlapBoxNonAlloc`, extracted `CurrentVelocity` from any `MovingPlatform` or `JumpingPlatform` detected beneath the cube's feet.
  - **Momentum Latching**: When the player takes off via jump (`jumpedThisStep`) or steps off an edge (`!grounded && wasGrounded`), latched the platform's velocity into `airbornePlatformMomentum`.
  - **Mid-Air Trajectory Preservation**: While airborne (`!grounded`), injected `airbornePlatformMomentum` into the horizontal velocity:
    $$v_x = \text{moveDir}_x \cdot v_{\text{move}} + v_{\text{airbornePlatform}, x}$$
    $$v_z = \text{moveDir}_z \cdot v_{\text{move}} + v_{\text{airbornePlatform}, z}$$
    This preserves full in-flight control: players retain full responsiveness on top of the platform's inertia (jumping in the direction of the platform adds speeds, jumping backwards counteracts it, jumping neutral maintains platform speed).
  - **Obstacle Impact Absorption**: In `OnCollisionEnter`, if the airborne cube strikes a wall while carrying platform momentum, projected the momentum onto the wall plane:
    ```csharp
    airbornePlatformMomentum = Vector3.ProjectOnPlane(airbornePlatformMomentum, contact.normal);
    ```
    preventing unnatural frictionless grinding against barriers.
  - **State Resets**: Reset `airbornePlatformMomentum` to zero immediately upon touchdown (`grounded`), emergency stop (`StopMovement()`), freeze (`SetFrozen(true)`), or respawn/teleport (`TeleportTo()`).







