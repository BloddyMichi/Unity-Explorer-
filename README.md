# UnityExplorer Data Center Safe Fork

A Data Center-focused UnityExplorer fork prepared for **MelonLoader v0.7.2 Open-Beta**, **Unity 6000.4.2f1**, **IL2CPP**, and **net6/CoreCLR**.

This fork is intended for runtime inspection and debugging in the Steam game **Data Center** by Waseku. It contains stability changes for Unity 6 / IL2CPP where the default UnityExplorer behavior can trigger unsafe Unity API calls.

## Main goal

Make UnityExplorer usable in Data Center without recurring crashes or console spam from unstable Unity 6000 / IL2CPP calls.

## Included Data Center fixes

- SafeInspector mode.
- Automatic property evaluation disabled.
- Object Explorer safe search mode.
- Scene Explorer disabled on Unity 6000 to avoid `SceneManager.sceneCount` access violations.
- Inspector panel auto-create fix.
- EventSystem spam reduction.
- Compact Inspector UI adjustments.
- Single-click UI button behavior improvement.
- Deferred panel bootstrap for Unity 6000.

## Build

Open PowerShell in the repository root and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-datacenter.ps1
```

The Data Center build output is expected at:

```text
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

## Install

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-datacenter.ps1
```

Default game path used by the install script:

```text
C:\Program Files (x86)\Steam\steamapps\common\Data Center
```

## Ready release asset

A ready-to-copy release ZIP from the uploaded source package is included in:

```text
release-assets\UnityExplorer.MelonLoader.IL2CPP.CoreCLR_FIXED.zip
```

## Notes

This fork intentionally disables or delays some default UnityExplorer features on Unity 6000. The normal Scene Explorer is replaced by a safer Object Search mode. Use Mouse Inspect and Object Search for Data Center object inspection.

Upstream UnityExplorer README is preserved here:

```text
docs\README_UPSTREAM_UnityExplorer.md
```
