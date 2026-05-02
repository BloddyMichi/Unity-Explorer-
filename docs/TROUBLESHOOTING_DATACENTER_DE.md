# Troubleshooting — Data Center

## Wichtigster Build

Für Data Center wird diese Variante verwendet:

```text
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR
```

`build-datacenter.ps1` baut gezielt nur diese CoreCLR-Variante. Der volle Upstream-Build ist für Data Center normalerweise nicht erforderlich.

## Dateien im Spielordner

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

## Erwartete sichere Meldungen

```text
UnityExplorer 4.13.5 (IL2CPP) initialized.
ObjectExplorerPanel: Unity 6000 safe mode skips Scene Explorer...
Object Search (Safe)
```

## Bekannte Einschränkung

Der normale Scene Explorer ist unter Unity 6000 deaktiviert. Nutze stattdessen:

- Inspector → Mouse Inspect
- Object Explorer → Object Search (Safe)
- gezielte `Evaluate`-Buttons statt Auto-Update
