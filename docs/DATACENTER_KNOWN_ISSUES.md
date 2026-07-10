# Data Center Known Issues

Generated: 2026-07-10 19:08:54 +02:00

## Detected Runtime

- [18:38:23.835] Unity Version: 6000.4.12f1
- [18:38:23.562] MelonLoader v0.7.3 Open-Beta
- [18:38:23.835] Game Version: UNKNOWN
- Game directory: C:\Program Files (x86)\Steam\steamapps\common\Data Center

## Support Matrix

| Area | Status | Notes |
|---|---|---|
| Unity 6000.x / IL2CPP / CoreCLR startup | Supported | Tested with Unity 6000.4.12f1 and MelonLoader 0.7.3 Open-Beta. |
| C# Console | Supported with fallback | Uses safe multi-line input on Unity 6000. |
| Hook Manager | Supported with fallback | Uses safe editor input and generic argument container on Unity 6000. |
| Object Search | Supported | Preferred replacement for Scene Explorer in Data Center. |
| Scene Explorer | Disabled by default | Disabled because SceneManager scene access can throw AccessViolationException on Unity 6000 IL2CPP/CoreCLR. |
| TimeScale widget | Disabled by default | Can be re-enabled from options if needed, but restart is recommended. |
| UniverseLib UI AssetBundle | Disabled by default | Built-in UI resources are used on Unity 6000. |

## Known Issues

- MelonLoader can log an early `EndOfStreamException` while reading game info. If UnityExplorer initializes afterwards, this is not currently blocking.
- Some Unity UIElements generic type cache warnings can appear during UniverseLib startup. They are logged as warnings and have not blocked the Data Center flow.
- Layer name lookup is avoided in Unity 6000 safe paths; numeric layer labels may appear instead.
- If UnityExplorer does not load, run `.\install-datacenter.ps1 -Status` and check for duplicate or misplaced DLLs.

## Useful Commands

```powershell
.\install-datacenter.ps1 -Status
.\install-datacenter.ps1 -BuildIfMissing -DryRun
.\install-datacenter.ps1 -Uninstall
.\install-datacenter.ps1 -RestoreBackup latest
.\collect-datacenter-logs.ps1
.\update-datacenter-known-issues.ps1
```
