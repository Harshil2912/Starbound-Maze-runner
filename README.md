# 🌌 Starbound: Maze Runner

[![Unity 6](https://img.shields.io/badge/Unity-6000.0-black?logo=unity)](https://unity.com/)
[![AR Foundation](https://img.shields.io/badge/AR_Foundation-Mobile_AR-blue)](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@latest)
[![Render Pipeline](https://img.shields.io/badge/Render_Pipeline-URP-orange)](https://unity.com/srp/Universal-Render-Pipeline)
[![Platform](https://img.shields.io/badge/Platform-Android_%7C_Windows-green?logo=android)](https://github.com/Harshil2912/starbound-maze-runner/releases/latest)
[![Download APK](https://img.shields.io/badge/Playable-Download_APK-brightgreen?logo=android)](https://github.com/Harshil2912/starbound-maze-runner/releases/latest)

A 5-level hybrid puzzle-adventure game combining high-precision 3D navigation with tabletop **Augmented Reality (AR)**. Developed in **Unity 6**, **C#**, and the **Universal Render Pipeline (URP)** using **AR Foundation**.

Switch seamlessly between full-screen 3D and real-world AR tabletop placement, navigating intricate cybernetic labyrinths with responsive ball physics, interactive puzzles, and dynamic hazards.

---

## 📱 Download & Play the APK (Android)

You can install and play the game directly on your Android phone (ARCore supported):

1. Go to the [**Releases Page**](https://github.com/Harshil2912/starbound-maze-runner/releases/latest).
2. Download **`Starbound - Maze Runner.apk`** (~39 MB).
3. Open the downloaded file on your Android device and tap **Install** *(allow "Install from Unknown Sources" if prompted)*.
4. Launch **Starbound: Maze Runner** and choose between **3D Normal Mode** or **AR Tabletop Mode**!

---

## ✨ Key Features

- **Dual-Mode Gameplay:** Seamlessly switch perspectives between full-screen 3D normal view and real-world tabletop AR.
- **Real-Time Surface Tracking:** Uses Unity AR Foundation for plane detection and instant level anchoring onto any flat surface.
- **6-DoF Holographic Orbit:** Walk 360° around floating miniature labyrinths with true physical parallax.
- **Physics-Driven Mechanics:** Responsive ball rolling physics, interactive pushable crates, pressure plates, and dynamic void chasms.
- **Procedural Hazards:** Timed laser barrier matrices, disappearing floor platforms, and crumbling bridge blocks.
- **Universal Render Pipeline (URP):** Vibrant neon aesthetics, bloom effects, dynamic shadows, and mobile performance optimization.

---

## 🗺️ 5-Level Campaign Overview

| Level | Title | Environment & Mechanics |
| :---: | :--- | :--- |
| **01** | **Ancient Sandstone Maze** | Core runner controls, pathfinding, and responsive ball physics. |
| **02** | **Basalt Abyss** | Traversal over void chasms, glowing jump triggers, and energy core pickups. |
| **03** | **Granite Temple & Weight Puzzle** | Pushable heavy physics crates and pressure plates controlling stone gates. |
| **04** | **Volcanic Magma Conduits** | High-stakes magma corridors with runtime 3D to AR surface repositioning. |
| **05** | **Event Horizon Diorama** | 9-sector cosmic station with synchronized laser matrices, crumbling tiles, and 360° AR orbit. |

---

## 🕹️ Controls

| Action | Mobile / Touch UI | Keyboard (Desktop) |
| :--- | :--- | :--- |
| **Move** | Responsive Virtual D-Pad (Up, Down, Left, Right) | `W`, `A`, `S`, `D` or `Arrow Keys` |
| **Stop** | Center `STOP` button | Release movement key |
| **Jump** | `JUMP` action button | `Spacebar` |
| **View Toggle** | Top-left Mode badge | Tap to toggle 3D Normal / AR Tabletop |
| **Reposition AR** | Top-right `REPOSITION` button | Re-scans real-world surface plane |

---

## 🛠️ Tech Stack & Architecture

- **Engine:** Unity 6 (6000.x)
- **Framework:** AR Foundation (ARCore / ARKit)
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Language:** C# (.NET Standard)
- **Input System:** Unity New Input System & Responsive Touch Canvas
- **Lighting & VFX:** PBR Lit Materials, Particle Systems, Custom Emission Shaders

```text
Maze Runner/
├── Assets/
│   ├── Editor/           # Level builders and scene polish utilities
│   ├── Materials/        # URP PBR materials, textures, and shaders
│   ├── Prefabs/          # Player sphere, collectibles, hazards, UI canvases
│   ├── Scenes/           # Active game scenes (PuzzleSolver.unity)
│   ├── Scripts/          # Core gameplay, AR controllers, physics, HUD
│   ├── Settings/         # URP asset pipelines & post-processing volumes
│   └── UI/               # Responsive HUD graphics, 9-slice frames
├── Packages/             # URP, AR Foundation, Input System dependencies
├── ProjectSettings/      # Graphics, physics, and input configs
├── .gitignore            # Clean repository rules
└── README.md
```

---

## 🚀 Opening the Project in Unity

1. Clone or download this repository.
2. Open **Unity Hub** and click **Add** -> **Add project from disk**.
3. Select the `Maze Runner` directory.
4. Ensure Unity 6 (or 2022.3+ LTS with URP support) is selected.
5. In the Project window, open `Assets/Scenes/PuzzleSolver.unity`.
6. Press the **Play (▶)** button to test in Editor!

---

## 📄 License & Credits
Developed by **Harshil Solanki** | Built with Unity & AR Foundation.
