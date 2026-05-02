# Installation — Data Center

## Deutsch

### Empfohlene Variante

Lade den neuesten Release herunter:

```text
https://github.com/BloddyMichi/Unity-Explorer-/releases/latest
```

Entpacke das Release-ZIP direkt in deinen Data-Center-Spielordner. Danach sollten diese Dateien existieren:

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

Typischer Steam-Pfad:

```text
C:\Program Files (x86)\Steam\steamapps\common\Data Center
```

### Update von einer älteren Version

Beende Data Center vollständig, bevor du Dateien ersetzt.

Wenn du eine alte Installation manuell aktualisierst, sichere oder entferne vorher diese Dateien:

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

Falls du aus dem Repository baust, kann das Install-Skript die Dateien kopieren und vorhandene DLLs automatisch sichern:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-datacenter.ps1 -BuildIfMissing
```

Bei einem anderen Installationspfad:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-datacenter.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Data Center" -BuildIfMissing
```

### Prüfen

Nach dem Start sollten MelonLoader und UnityExplorer im Log sichtbar sein:

```text
Data Center\MelonLoader\Latest.log
```

Erwartete Hinweise:

```text
UnityExplorer 4.13.5 (IL2CPP) initialized.
ObjectExplorerPanel: Unity 6000 safe mode skips Scene Explorer...
Object Search (Safe)
```

## English

### Recommended path

Download the latest release:

```text
https://github.com/BloddyMichi/Unity-Explorer-/releases/latest
```

Extract the release ZIP directly into your Data Center game folder. These files should exist afterwards:

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

Typical Steam path:

```text
C:\Program Files (x86)\Steam\steamapps\common\Data Center
```

### Updating from an older version

Close Data Center completely before replacing files.

If you update an old installation manually, back up or remove these files first:

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

If you build from the repository, the install script can copy the files and back up existing DLLs automatically:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-datacenter.ps1 -BuildIfMissing
```

For a custom install path:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-datacenter.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Data Center" -BuildIfMissing
```

### Verify

After starting the game, MelonLoader and UnityExplorer should appear in the log:

```text
Data Center\MelonLoader\Latest.log
```

Expected messages:

```text
UnityExplorer 4.13.5 (IL2CPP) initialized.
ObjectExplorerPanel: Unity 6000 safe mode skips Scene Explorer...
Object Search (Safe)
```
