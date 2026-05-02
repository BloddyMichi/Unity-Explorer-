# Troubleshooting — Data Center

## Deutsch

### Wichtigster Build

Für Data Center wird diese Variante verwendet:

```text
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR
```

`build-datacenter.ps1` baut gezielt nur diese CoreCLR-Variante. Der volle Upstream-Build ist für Data Center normalerweise nicht erforderlich.

### Dateien im Spielordner

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

### Erwartete sichere Meldungen

```text
UnityExplorer 4.13.5 (IL2CPP) initialized.
ObjectExplorerPanel: Unity 6000 safe mode skips Scene Explorer...
Unity 6000 detected, using safe C# Console input fallback.
Unity 6000 detected, using safe Hook Editor input fallback.
Unity 6000 detected, using safe Generic Arguments container fallback.
Object Search (Safe)
```

### Häufige Probleme

**UnityExplorer erscheint nicht**

- Prüfe `Data Center\MelonLoader\Latest.log`.
- Prüfe, ob die DLL wirklich im `Mods`-Ordner liegt.
- Prüfe, ob `UniverseLib.ML.IL2CPP.Interop.dll` im `UserLibs`-Ordner liegt.
- Entferne alte UnityExplorer- oder UniverseLib-Dateien aus falschen Ordnern.

**Button-Klicks oder Mausfokus wirken unzuverlässig**

- Öffne UnityExplorer erst nach dem Laden des Spielstands.
- Schließe Spielmenüs oder Overlays, die Eingaben abfangen.
- Nutze Mouse Inspect nur gezielt und schließe das Panel danach wieder.

**Scene Explorer fehlt**

Der normale Scene Explorer ist unter Unity 6000 deaktiviert. Nutze stattdessen:

- Inspector → Mouse Inspect
- Object Explorer → Object Search (Safe)
- gezielte `Evaluate`-Buttons statt Auto-Update

**C# Console oder HookManager nutzen vereinfachte Eingabefelder**

Unter Unity 6000 werden einige scrollbare UniverseLib-UI-Elemente bewusst ersetzt, weil sie auf IL2CPP/CoreCLR `AccessViolationException` auslösen können.

- C# Console nutzt ein sicheres normales Eingabefeld.
- HookManager Editor nutzt ein sicheres normales Eingabefeld.
- Generic Arguments nutzt einen einfachen Container statt `ScrollView`.
- Das ist erwartet, wenn die Meldungen oben im Log erscheinen.

**MelonLoader startet, aber nach einem Update sind alte Dateien aktiv**

- Beende Data Center vollständig, bevor du DLLs ersetzt.
- Prüfe, ob genau diese Dateien aktualisiert wurden:

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

- Wenn du das Install-Skript verwendest, prüfe den letzten Ordner unter `Data Center\UnityExplorer.Backups`.

## English

### Main build

Data Center uses this build:

```text
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR
```

`build-datacenter.ps1` only builds this CoreCLR variant. The full upstream build is usually not needed for Data Center.

### Files in the game folder

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

### Expected safe messages

```text
UnityExplorer 4.13.5 (IL2CPP) initialized.
ObjectExplorerPanel: Unity 6000 safe mode skips Scene Explorer...
Unity 6000 detected, using safe C# Console input fallback.
Unity 6000 detected, using safe Hook Editor input fallback.
Unity 6000 detected, using safe Generic Arguments container fallback.
Object Search (Safe)
```

### Common issues

**UnityExplorer does not appear**

- Check `Data Center\MelonLoader\Latest.log`.
- Check that the UnityExplorer DLL is really in the `Mods` folder.
- Check that `UniverseLib.ML.IL2CPP.Interop.dll` is in the `UserLibs` folder.
- Remove old UnityExplorer or UniverseLib files from wrong folders.

**Button clicks or mouse focus feel unreliable**

- Open UnityExplorer after the save game has finished loading.
- Close game menus or overlays that may capture input.
- Use Mouse Inspect only when needed and close the panel afterwards.

**Scene Explorer is missing**

The regular Scene Explorer is disabled on Unity 6000. Use these instead:

- Inspector → Mouse Inspect
- Object Explorer → Object Search (Safe)
- targeted `Evaluate` buttons instead of auto-update

**C# Console or HookManager use simplified input fields**

On Unity 6000, some scrollable UniverseLib UI elements are intentionally replaced because they can trigger `AccessViolationException` on IL2CPP/CoreCLR.

- C# Console uses a safe standard input field.
- HookManager Editor uses a safe standard input field.
- Generic Arguments uses a simple container instead of `ScrollView`.
- This is expected when the messages above appear in the log.

**MelonLoader starts, but old files are still active after an update**

- Close Data Center completely before replacing DLLs.
- Check that these exact files were updated:

```text
Data Center\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Data Center\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
```

- If you use the install script, check the latest folder under `Data Center\UnityExplorer.Backups`.
