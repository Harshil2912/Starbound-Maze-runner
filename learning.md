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

