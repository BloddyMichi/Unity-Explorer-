# UnityExplorer – Data Center Safe Fork

<p align="center">
  <b>UnityExplorer build prepared for Data Center, Unity 6000, IL2CPP and MelonLoader CoreCLR.</b>
</p>

<p align="center">
  <img alt="Game" src="https://img.shields.io/badge/Game-Data%20Center-2f8f46?style=for-the-badge">
  <img alt="Unity" src="https://img.shields.io/badge/Unity-6000.4.2f1-222222?style=for-the-badge&logo=unity">
  <img alt="MelonLoader" src="https://img.shields.io/badge/MelonLoader-0.7.2%20Open--Beta-bb3f3f?style=for-the-badge">
  <img alt="Runtime" src="https://img.shields.io/badge/Runtime-net6%20%2F%20CoreCLR-512bd4?style=for-the-badge&logo=dotnet">
  <img alt="License" src="https://img.shields.io/badge/License-GPL--3.0-blue?style=for-the-badge">
</p>

---

## Overview

This repository contains a **Data Center focused UnityExplorer fork** for runtime inspection and debugging in the Steam game **Data Center** by Waseku.

The goal is to make UnityExplorer usable on **Unity 6000.4.2f1 / IL2CPP / net6 CoreCLR** without the recurring crashes, console spam, and unsafe Unity API calls that can happen with the default UnityExplorer behavior.

> This is a compatibility-focused fork. Some default UnityExplorer features are intentionally disabled, delayed, or replaced with safer alternatives for Data Center.

---

## Compatibility

| Component | Target |
|---|---|
| Game | Data Center |
| Developer | Waseku |
| Unity | 6000.4.2f1 |
| Game backend | IL2CPP |
| Runtime | net6 / CoreCLR |
| Loader | MelonLoader v0.7.2 Open-Beta |
| Main output | `UnityExplorer.ML.IL2CPP.CoreCLR.dll` |

---

## Main fixes

### Stability

- SafeInspector mode for Unity 6000 / IL2CPP.
- Automatic property evaluation disabled to avoid unsafe getters.
- `Transform.hasChanged` auto-evaluation crash prevention.
- `SceneManager.sceneCount` access violation prevention.
- Deferred panel bootstrap for safer UI initialization.
- Reduced repeated EventSystem warning spam.

### Object Explorer

- Scene Explorer is skipped on Unity 6000.
- Safe Object Search panel is used instead.
- Object search works with class/name filters.
- Safer inspection flow for Data Center objects such as racks, switches, walls, devices and UI objects.

### UI / UX

- More compact Inspector panel layout.
- Improved single-click button behavior.
- Delayed creation of heavy panels until they are opened.
- Cleaner Data Center oriented startup behavior.

---

## Quick build

Open PowerShell in the repository root and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-datacenter.ps1
```

Expected Data Center output:

```text
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

> `build-datacenter.ps1` builds only the CoreCLR IL2CPP MelonLoader target needed for Data Center and creates `Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip`.

---

## Install

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-datacenter.ps1
```

Default game path used by the install script:

```text
C:\Program Files (x86)\Steam\steamapps\common\Data Center
```

Expected install layout:

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

---

## Ready release asset

A ready-to-copy release ZIP from the prepared source package is included here:

```text
release-assets\UnityExplorer.MelonLoader.IL2CPP.CoreCLR_FIXED.zip
```

Use this ZIP when you only want the already prepared Data Center build files instead of rebuilding from source.

---

## Usage notes for Data Center

Recommended workflow inside the game:

1. Open UnityExplorer with the configured hotkey.
2. Use **Object Explorer**.
3. Search by object/class name, for example `Rack`, `Switch`, `Cable`, `Wall`, `Customer`, or `Server`.
4. Inspect objects through the safer Inspector flow.
5. Avoid forcing unsafe global scene scans on Unity 6000.

---

## Known limitations

| Area | Status |
|---|---|
| Scene Explorer | Disabled on Unity 6000 to avoid `SceneManager.sceneCount` crashes. |
| Automatic property evaluation | Disabled for safety. Use manual evaluation only when needed. |
| Mono targets | Not required for Data Center and may fail during full upstream build. |
| Unity 6000 IL2CPP | Uses compatibility workarounds because some APIs are unstable through reflection/interop. |

---

## Project layout

```text
.github/                 GitHub files and templates
UnityEditorPackage/       Upstream UnityExplorer editor package files
UniverseLib/              UniverseLib source
src/                      UnityExplorer source
lib/                      Library/dependency files
docs/                     Documentation and upstream README backup
img/                      Images and documentation assets
release-assets/           Prepared release ZIPs
build.ps1                 Original/full build script
build-datacenter.ps1      Data Center focused build script
install-datacenter.ps1    Data Center install helper
THIRDPARTY_LICENSES.md    Third-party license notes
```

---

## Änderungsübersicht / Change summary

### Deutsch

- Data-Center-spezifische GitHub-Struktur erstellt.
- SafeInspector-Modus ergänzt.
- Automatische Property-Auswertung deaktiviert.
- Object Explorer auf Safe Search umgestellt.
- Scene Explorer unter Unity 6000 deaktiviert.
- Schutz gegen `SceneManager.sceneCount` AccessViolation ergänzt.
- Inspector Auto-Create Verhalten verbessert.
- UI kompakter gemacht.
- Button-Verhalten auf einfaches Klicken verbessert.
- Build- und Install-Scripts für Data Center ergänzt.
- Fertiges Release-ZIP in `release-assets` abgelegt.

### English

- Created a Data Center focused GitHub repository structure.
- Added SafeInspector mode.
- Disabled automatic property evaluation.
- Reworked Object Explorer to use safe search mode.
- Disabled Scene Explorer on Unity 6000.
- Added protection against `SceneManager.sceneCount` access violations.
- Improved Inspector auto-create behavior.
- Made the UI more compact.
- Improved single-click button behavior.
- Added Data Center build and install scripts.
- Added a prepared release ZIP in `release-assets`.

---

## Upstream README

The original upstream UnityExplorer README is preserved here:

```text
docs\README_UPSTREAM_UnityExplorer.md
```

---

## Credits

- Original UnityExplorer by Sinai and contributors.
- UniverseLib by Sinai and contributors.
- Data Center compatibility fork prepared by Bloody.

---

## License

This repository keeps the upstream **GPL-3.0** license.
