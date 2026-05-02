# Änderungs-/Change List — Data Center Safe Fork

## Deutsch

- UnityExplorer für **Data Center** angepasst.
- Kompatibilität für **MelonLoader v0.7.2 Open-Beta** verbessert.
- Anpassungen für **Unity 6000.4.2f1 / IL2CPP / net6 CoreCLR** eingebaut.
- SafeInspector-Modus hinzugefügt.
- Automatische Property-Auswertung deaktiviert, um Fehler bei `Transform.hasChanged` zu vermeiden.
- `SceneManager.sceneCount`-Zugriffe im Object Explorer entschärft.
- Scene Explorer unter Unity 6000 deaktiviert, um `AccessViolationException` zu vermeiden.
- Object Explorer auf sicheren Suchmodus umgestellt: `Object Search (Safe)`.
- Inspector-Panel wird bei Bedarf automatisch erstellt, wenn es noch nicht registriert ist.
- Fehler behoben: `KeyNotFoundException: Inspector was not present in the dictionary`.
- EventSystem-Spam reduziert bzw. unterdrückt.
- Unity-6000-Safe-Mode für instabile UI- und Scene-Funktionen ergänzt.
- Inspector-Layout kompakter gemacht.
- Mouse-Inspect-Overlay verkleinert.
- UI-Buttons kompakter angeordnet.
- Button-Verhalten verbessert, damit ein Klick zuverlässiger ausreicht.
- Deferred Panel Bootstrap für Unity 6000 eingebaut.
- ConsoleController wird erst bei Bedarf initialisiert.
- DisplayManager-Initialisierung unter Unity 6000 übersprungen.
- TimeScaleWidget unter Unity 6000 deaktiviert.
- Built-in UI-Ressourcen werden verwendet, wenn das UniverseLib AssetBundle unter Unity 6000 instabil ist.
- Data-Center-spezifisches Build-Script hinzugefügt.
- Data-Center-spezifisches Install-Script hinzugefügt.
- GitHub-Repo-Struktur vorbereitet.
- GitHub Actions Workflow für CoreCLR-Build hinzugefügt.
- Upstream-README separat unter `docs/README_UPSTREAM_UnityExplorer.md` abgelegt.
- Fertiges CoreCLR-Release-ZIP unter `release-assets/` beigelegt.

## English

- Adapted UnityExplorer for **Data Center**.
- Improved compatibility with **MelonLoader v0.7.2 Open-Beta**.
- Added adjustments for **Unity 6000.4.2f1 / IL2CPP / net6 CoreCLR**.
- Added SafeInspector mode.
- Disabled automatic property evaluation to avoid `Transform.hasChanged` errors.
- Hardened Object Explorer against unsafe `SceneManager.sceneCount` calls.
- Disabled Scene Explorer on Unity 6000 to avoid `AccessViolationException`.
- Switched Object Explorer to a safe search-only mode: `Object Search (Safe)`.
- Inspector panel is now auto-created when requested but not yet registered.
- Fixed `KeyNotFoundException: Inspector was not present in the dictionary`.
- Reduced / suppressed EventSystem log spam.
- Added Unity 6000 safe mode handling for unstable UI and scene functions.
- Made the Inspector layout more compact.
- Reduced the Mouse Inspect overlay size.
- Made UI buttons more compact.
- Improved button behavior so a single click is more reliable.
- Added deferred panel bootstrap for Unity 6000.
- ConsoleController initialization is delayed until the console is opened.
- Skipped DisplayManager initialization on Unity 6000.
- Disabled TimeScaleWidget on Unity 6000.
- Uses built-in UI resources when the UniverseLib UI AssetBundle is unstable on Unity 6000.
- Added Data Center-specific build script for the CoreCLR IL2CPP MelonLoader target.
- Added Data Center-specific install script.
- Prepared a clean GitHub repository structure.
- Added GitHub Actions workflow for CoreCLR builds.
- Preserved upstream README as `docs/README_UPSTREAM_UnityExplorer.md`.
- Included ready CoreCLR release ZIP under `release-assets/`.
