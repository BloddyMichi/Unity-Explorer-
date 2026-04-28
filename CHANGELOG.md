# Changelog

All notable changes to this Data Center-focused UnityExplorer fork are documented here.

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
- Unity: 6000.4.2f1
- Loader: MelonLoader v0.7.2 Open-Beta
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
