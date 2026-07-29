# Changelog

All notable changes to this Data Center-focused UnityExplorer fork are documented here.

## Unreleased

### English

- Repository housekeeping: fixed the corrupted (mojibake) `src/.editorconfig` and rewrote it as clean UTF-8.
- Aligned the Unity editor package version (`UnityEditorPackage/package.json`) with the assembly version `4.13.5`.
- Documented the version scheme: `4.13.5` is the upstream UnityExplorer base; the fork release series (`v1.x`) lives in this changelog.
- Pinned the .NET SDK via `global.json` for reproducible builds.
- Unified `Microsoft.Unity.Analyzers` to `1.19.0` across both projects.
- Added a `CONTRIBUTING.md` with branch, build and style guidance.
- Added a pull-request CI workflow that compiles the Data Center CoreCLR target and runs unit tests.
- Added a first unit-test project (`tests/UnityExplorer.Tests`) covering platform-independent utility logic.
- Added a support package script for collecting Data Center logs, file hashes and detected runtime versions.
- Improved `install-datacenter.ps1` with dry-run, wait-for-exit, MelonLoader preflight and duplicate-file warnings.
- Added a manual GitHub Actions release workflow for Data Center CoreCLR builds.
- Centralized Unity 6000 safe UI fallback helpers.
- Added clearer runtime compatibility logging.
- GitHub Actions now uploads the ready-to-use CoreCLR ZIP plus generated SHA256 checksums.
- `build-datacenter.ps1` now writes `Release\CHECKSUMS_SHA256.txt` for the ZIP and installed DLLs.
- Added local bilingual installation and troubleshooting docs for users who do not use the wiki.
- Cleaned up duplicate GitHub issue templates and fixed the installation guide contact link.

### Deutsch

- Repository-Aufräumarbeiten: beschädigte (Mojibake-)`src/.editorconfig` behoben und als sauberes UTF-8 neu geschrieben.
- Version des Unity-Editor-Pakets (`UnityEditorPackage/package.json`) an die Assembly-Version `4.13.5` angeglichen.
- Versionsschema dokumentiert: `4.13.5` ist die UnityExplorer-Upstream-Basis; die Fork-Release-Serie (`v1.x`) steht in diesem Changelog.
- .NET-SDK über `global.json` für reproduzierbare Builds gepinnt.
- `Microsoft.Unity.Analyzers` in beiden Projekten auf `1.19.0` vereinheitlicht.
- `CONTRIBUTING.md` mit Branch-, Build- und Style-Hinweisen ergänzt.
- Pull-Request-CI-Workflow ergänzt, der das Data-Center-CoreCLR-Ziel kompiliert und Unit-Tests ausführt.
- Erstes Unit-Test-Projekt (`tests/UnityExplorer.Tests`) für plattformunabhängige Utility-Logik hinzugefügt.
- Support-Paket-Skript zum Sammeln von Data-Center-Logs, Datei-Hashes und erkannten Runtime-Versionen hinzugefügt.
- `install-datacenter.ps1` mit Dry-Run, Warten auf Spielende, MelonLoader-Vorprüfung und Warnungen vor doppelten Dateien verbessert.
- Manuellen GitHub-Actions-Release-Workflow für Data-Center-CoreCLR-Builds hinzugefügt.
- Unity-6000-Safe-UI-Fallback-Helfer zentralisiert.
- Klareres Runtime-Kompatibilitätslogging ergänzt.
- GitHub Actions lädt jetzt die fertige CoreCLR-ZIP plus erzeugte SHA256-Prüfsummen hoch.
- `build-datacenter.ps1` schreibt jetzt `Release\CHECKSUMS_SHA256.txt` für ZIP und installierbare DLLs.
- Lokale zweisprachige Installations- und Troubleshooting-Dokumente für Nutzer ohne Wiki ergänzt.
- Doppelte GitHub-Issue-Vorlagen bereinigt und den Kontaktlink zur Installationsanleitung korrigiert.

### Dependency updates (pending in-game verification) / Dependency-Updates (In-Game-Verifikation ausstehend)

> These bumps were prepared as isolated commits and have **not** been compiled or
> tested against Data Center yet. Verify each in-game (see `CONTRIBUTING.md`)
> before releasing; revert individually if a game regression appears.
>
> Diese Bumps wurden als isolierte Commits vorbereitet und sind noch **nicht**
> kompiliert oder gegen Data Center getestet. Vor einem Release jeweils im Spiel
> verifizieren (siehe `CONTRIBUTING.md`); bei einer Regression einzeln zurückrollen.

- **Il2CppInterop `1.0.0` → `1.5.1`** (`src` and `UniverseLib`, Interop/CoreCLR configs) to match the Il2CppInterop runtime bundled with MelonLoader 0.7.3 (`1.5.1-ci.845`).
- **HarmonyX `2.5.2` → `2.14.0`** (`src` and `UniverseLib`, compile-only). Runtime patching still uses MelonLoader's force-resolved `0Harmony.dll`; only the compile-time API surface changes.

## v1.0.1 - CoreCLR build cleanup

### English

- Focused `build-datacenter.ps1` on the Data Center IL2CPP CoreCLR MelonLoader target.
- Added direct packaging for the expected `Mods` and `UserLibs` output structure.
- Refreshed the ready-to-use CoreCLR release ZIP and SHA256 checksum.
- Removed the duplicate bug report issue template.
- Fixed a nullable warning in `SceneHandler`.
- Merged the latest bilingual README improvements from `main`.

### Deutsch

- `build-datacenter.ps1` auf das Data-Center-Ziel IL2CPP CoreCLR MelonLoader fokussiert.
- Direkte Paketierung für die erwartete `Mods`- und `UserLibs`-Ordnerstruktur ergänzt.
- Fertige CoreCLR-Release-ZIP und SHA256-Prüfsumme aktualisiert.
- Doppelte Bug-Report-Issue-Vorlage entfernt.
- Nullable-Warnung in `SceneHandler` behoben.
- Neueste zweisprachige README-Verbesserungen aus `main` übernommen.

## v1.0.0 - Initial Data Center Safe Fork

### English

- Added Data Center safe fork setup.
- Added Unity 6000 / IL2CPP / CoreCLR stability changes.
- Disabled unsafe Scene Explorer behavior on Unity 6000.
- Added Object Explorer safe search mode.
- Reduced `SceneManager.sceneCount` access violation risk.
- Disabled automatic property evaluation for safer inspection.
- Added Inspector panel auto-create fix.
- Reduced EventSystem spam.
- Improved single-click UI button behavior.
- Added compact Inspector layout adjustments.
- Added Data Center build and install helper scripts.
- Added ready-to-use binary release ZIP for end users.

### Deutsch

- Data-Center-Safe-Fork eingerichtet.
- Stabilitätsanpassungen für Unity 6000 / IL2CPP / CoreCLR hinzugefügt.
- Unsicheren Scene Explorer unter Unity 6000 deaktiviert.
- Sicheren Object-Explorer-Suchmodus hinzugefügt.
- Risiko für `SceneManager.sceneCount` AccessViolation reduziert.
- Automatische Property-Auswertung deaktiviert.
- Inspector-Panel-Auto-Erstellung korrigiert.
- EventSystem-Spam reduziert.
- Button-Verhalten verbessert, damit ein Klick reicht.
- Kompakteres Inspector-Layout eingebaut.
- Build- und Install-Skripte für Data Center ergänzt.
- Fertige Nutzer-Release-ZIP für Endnutzer hinzugefügt.

### Compatibility / Kompatibilität

- Game: Data Center
- Unity: 6000.x, tested with 6000.4.12f1
- Loader: MelonLoader v0.7.x Open-Beta, tested with 0.7.3
- Runtime: net6 / CoreCLR
- Game type: IL2CPP

### Notes / Hinweise

This fork intentionally disables or delays some default UnityExplorer features on Unity 6000 to avoid unstable Unity API calls.

For Data Center object inspection, use:

- Object Explorer safe search
- Mouse Inspect
- Inspector

Dieser Fork deaktiviert oder verzögert bewusst einige Standardfunktionen von UnityExplorer unter Unity 6000, um instabile Unity-API-Aufrufe zu vermeiden.

Für die Objekt-Inspektion in Data Center sollten vor allem diese Funktionen genutzt werden:

- Object Explorer Safe Search
- Mouse Inspect
- Inspector

**Full Changelog**: https://github.com/BloddyMichi/Unity-Explorer-/commits/v1.0.0
