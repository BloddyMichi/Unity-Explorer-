# UnityExplorer – Data Center Safe Fork

<p align="center">
  <strong>Kompatibilitäts-Fork von UnityExplorer für Data Center, Unity 6000, IL2CPP und MelonLoader CoreCLR.</strong><br>
  <strong>Compatibility-focused UnityExplorer fork for Data Center, Unity 6000, IL2CPP and MelonLoader CoreCLR.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Game-Data%20Center-brightgreen" />
  <img src="https://img.shields.io/badge/Unity-6000.4.2f1-blue" />
  <img src="https://img.shields.io/badge/Backend-IL2CPP-orange" />
  <img src="https://img.shields.io/badge/Runtime-net6%20%2F%20CoreCLR-purple" />
  <img src="https://img.shields.io/badge/MelonLoader-0.7.2%20Open--Beta-red" />
  <img src="https://img.shields.io/badge/License-GPL--3.0-blue" />
</p>

---

## 🇩🇪 Deutsch

## Übersicht

Dieses Repository enthält einen auf **Data Center** angepassten **UnityExplorer-Fork** für Runtime-Inspektion, Debugging und Mod-Entwicklung im Steam-Spiel **Data Center** von Waseku.

Ziel dieses Forks ist es, UnityExplorer mit folgender Umgebung nutzbar zu machen:

- **Unity 6000**
- **IL2CPP**
- **net6 / CoreCLR**
- **MelonLoader 0.7.2 Open-Beta**
- **Data Center**

Dieser Fork ist auf Kompatibilität und Stabilität ausgelegt. Einige Standardfunktionen von UnityExplorer können angepasst, verzögert, deaktiviert oder durch sicherere Alternativen ersetzt sein, um Abstürze, Konsolen-Spam oder unsichere Unity-API-Aufrufe zu reduzieren.

---

## Kompatibilität

| Komponente | Ziel |
|---|---|
| Spiel | Data Center |
| Entwickler | Waseku |
| Unity | 6000.4.2f1 |
| Backend | IL2CPP |
| Runtime | net6 / CoreCLR |
| Loader | MelonLoader 0.7.2 Open-Beta |
| Plattform | Windows |
| Lizenz | GPL-3.0 |

---

## Funktionen

- Runtime-Inspektion für Data Center
- GameObject- und Komponenten-Browser
- Szenen-Inspektion
- Anzeige von Feldern, Werten und Objekten
- Analyse von Methoden und Komponenten
- IL2CPP-kompatibles Debugging
- MelonLoader-kompatibler Build
- Data-Center-spezifische Kompatibilitätsanpassungen
- vorbereitete Release-Pakete
- Wiki-Dokumentation
- Issue-Templates für Fehlerberichte und Feature Requests

---

## Projektstatus

Dieser Fork richtet sich an **Modder, Entwickler und Tester**, die Data Center zur Laufzeit untersuchen möchten.

Aktueller Fokus:

- Data-Center-Kompatibilität
- stabile MelonLoader-Integration
- Input- und Mouse-Focus-Tests
- sicherere Runtime-Inspektion
- saubere Release-Pakete
- bessere Dokumentation

Bekannter Hinweis:

> Mouse- oder Input-Fokus kann je nach Spielzustand, Szene oder UI-Fokus noch unterschiedlich reagieren.

---

## Installation

Lade den neuesten Release herunter:

[Releases](https://github.com/BloddyMichi/Unity-Explorer-/releases)

Kopiere die UnityExplorer-Dateien in den `Mods`-Ordner von Data Center.

Typischer Steam-Pfad:

```text
C:\Program Files (x86)\Steam\steamapps\common\Data Center\Mods
```

Vor der Installation einer neuen Version sollten alte UnityExplorer- oder UniverseLib-Dateien aus dem `Mods`-Ordner entfernt werden.

Wenn du aus diesem Repository baust, kann das Install-Skript die Dateien direkt in deinen Data-Center-Ordner kopieren. Vorhandene Dateien werden dabei gesichert:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-datacenter.ps1 -BuildIfMissing
```

Ausführliche Anleitung:

[Installationsanleitung](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Installation)

---

## Nutzung

Starte **Data Center** über Steam, nachdem MelonLoader und dieser UnityExplorer-Build installiert wurden.

UnityExplorer wird häufig mit folgender Taste geöffnet:

```text
F7
```

Falls UnityExplorer nicht erscheint, prüfe:

- ob MelonLoader korrekt startet
- ob die Dateien im richtigen `Mods`-Ordner liegen
- ob alte UnityExplorer-Dateien entfernt wurden
- ob die MelonLoader-Log Fehler enthält

Wichtige Log-Datei:

```text
Data Center\MelonLoader\Latest.log
```

---

## Aus dem Source Code bauen

Repository klonen:

```powershell
git clone https://github.com/BloddyMichi/Unity-Explorer-.git
cd Unity-Explorer-
```

Normales Build-Script starten:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Data-Center-spezifisches Build-Script:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-datacenter.ps1
```

Falls PowerShell das Script blockiert:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
```

Weitere Informationen:

[Build Guide](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Build-Guide)

---

## Dokumentation

Weitere Informationen findest du im Wiki:

- [Lokale Installation / Local Installation](docs/INSTALL_DATACENTER_DE_EN.md)
- [Lokales Troubleshooting / Local Troubleshooting](docs/TROUBLESHOOTING_DATACENTER_DE.md)
- [Home](https://github.com/BloddyMichi/Unity-Explorer-/wiki)
- [Installation](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Installation)
- [Known Issues](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Known-Issues)
- [FAQ](https://github.com/BloddyMichi/Unity-Explorer-/wiki/FAQ)
- [Build Guide](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Build-Guide)
- [Changelog](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Changelog)

---

## Fehler melden

Wenn du einen Fehler findest, erstelle bitte ein Issue:

[Issue erstellen](https://github.com/BloddyMichi/Unity-Explorer-/issues)

Bitte füge möglichst viele Informationen hinzu:

- Was ist passiert?
- Wann ist es passiert?
- Data-Center-Version
- MelonLoader-Version
- UnityExplorer-Version
- Screenshot, falls möglich
- `Latest.log`
- Schritte zum Reproduzieren

---

## Community / Support

Für Fragen, Feedback, Fehlerberichte oder Modding-Diskussionen kannst du dem Discord-Modding-Server beitreten:

[Discord beitreten](https://discord.gg/cRS4bCKUbe)

---

## Roadmap

Geplante oder laufende Arbeiten:

- Input- und Mouse-Focus-Verhalten verbessern
- weitere Data-Center-Updates testen
- Installationsdokumentation verbessern
- Release-ZIP-Struktur prüfen
- bekannte Probleme genauer dokumentieren
- bessere Hinweise für MelonLoader-Nutzer ergänzen

Projekt-Board:

[Unity Explorer – Data Center Roadmap](https://github.com/users/BloddyMichi/projects/1)

---

## Hinweis

Dies ist ein Community-Fork und kein offizielles Tool der Data-Center-Entwickler.

Die Nutzung erfolgt auf eigene Verantwortung.
Dieses Tool ist für Debugging, Modding, Tests und Entwicklung gedacht.

Verwende Runtime-Inspektoren oder Modding-Tools nicht in Umgebungen, in denen sie nicht erlaubt sind.

---

## Lizenz

Dieses Projekt steht unter der **GPL-3.0 License**.

Siehe:

[LICENSE](./LICENSE)

---

## Credits

Basierend auf UnityExplorer und verwandten Unity-Modding-Tools.

Gepflegt von:

**Bloody / BloddyMichi**

---

# 🇬🇧 English

## Overview

This repository contains a **Data Center focused UnityExplorer fork** for runtime inspection, debugging and mod development in the Steam game **Data Center** by Waseku.

The goal of this fork is to make UnityExplorer usable with the following environment:

- **Unity 6000**
- **IL2CPP**
- **net6 / CoreCLR**
- **MelonLoader 0.7.2 Open-Beta**
- **Data Center**

This fork focuses on compatibility and stability. Some default UnityExplorer behavior may be changed, delayed, disabled or replaced with safer alternatives to reduce crashes, console spam and unsafe Unity API calls.

---

## Compatibility

| Component | Target |
|---|---|
| Game | Data Center |
| Developer | Waseku |
| Unity | 6000.4.2f1 |
| Backend | IL2CPP |
| Runtime | net6 / CoreCLR |
| Loader | MelonLoader 0.7.2 Open-Beta |
| Platform | Windows |
| License | GPL-3.0 |

---

## Features

- Runtime inspection for Data Center
- GameObject and component browsing
- Scene inspection
- Field, value and object inspection
- Method and component analysis
- IL2CPP-compatible debugging
- MelonLoader-compatible build
- Data Center focused compatibility changes
- prepared release packages
- wiki documentation
- issue templates for bug reports and feature requests

---

## Project Status

This fork is intended for **modders, developers and testers** who want to inspect Data Center at runtime.

Current focus:

- Data Center compatibility
- stable MelonLoader integration
- input and mouse focus testing
- safer runtime inspection
- clean release packages
- better documentation

Known note:

> Mouse or input focus may still behave differently depending on the current game state, scene or UI focus.

---

## Installation

Download the latest release:

[Releases](https://github.com/BloddyMichi/Unity-Explorer-/releases)

Copy the UnityExplorer files into the Data Center `Mods` folder.

Typical Steam path:

```text
C:\Program Files (x86)\Steam\steamapps\common\Data Center\Mods
```

Before installing a new version, remove old UnityExplorer or UniverseLib files from the `Mods` folder.

If you build from this repository, the install script can copy the files directly into your Data Center folder. Existing files are backed up first:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-datacenter.ps1 -BuildIfMissing
```

Detailed instructions:

[Installation Guide](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Installation)

---

## Basic Usage

Start **Data Center** through Steam after installing MelonLoader and this UnityExplorer build.

UnityExplorer is commonly opened with:

```text
F7
```

If UnityExplorer does not appear, check:

- whether MelonLoader starts correctly
- whether the files are inside the correct `Mods` folder
- whether old UnityExplorer files were removed
- whether the MelonLoader log contains errors

Important log file:

```text
Data Center\MelonLoader\Latest.log
```

---

## Build From Source

Clone the repository:

```powershell
git clone https://github.com/BloddyMichi/Unity-Explorer-.git
cd Unity-Explorer-
```

Run the normal build script:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Run the Data Center specific build script:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-datacenter.ps1
```

Expected Data Center output:

```text
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR\Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR\UserLibs\UniverseLib.ML.IL2CPP.Interop.dll
Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip
Release\CHECKSUMS_SHA256.txt
```

> `build-datacenter.ps1` builds only the CoreCLR IL2CPP MelonLoader target needed for Data Center and creates `Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip`.

If PowerShell blocks the script:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
```

More information:

[Build Guide](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Build-Guide)

---

## Documentation

The wiki contains additional setup, troubleshooting and development information:

- [Local Installation](docs/INSTALL_DATACENTER_DE_EN.md)
- [Local Troubleshooting](docs/TROUBLESHOOTING_DATACENTER_DE.md)
- [Home](https://github.com/BloddyMichi/Unity-Explorer-/wiki)
- [Installation](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Installation)
- [Known Issues](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Known-Issues)
- [FAQ](https://github.com/BloddyMichi/Unity-Explorer-/wiki/FAQ)
- [Build Guide](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Build-Guide)
- [Changelog](https://github.com/BloddyMichi/Unity-Explorer-/wiki/Changelog)

---

## Reporting Bugs

Please open an issue if you find a problem:

[Create an Issue](https://github.com/BloddyMichi/Unity-Explorer-/issues)

Please include as much information as possible:

- what happened
- when it happened
- Data Center version
- MelonLoader version
- UnityExplorer version
- screenshot if possible
- `Latest.log`
- steps to reproduce the issue

---

## Community / Support

For questions, feedback, bug reports or modding discussions, you can join the Discord modding server:

[Join Discord](https://discord.gg/cRS4bCKUbe)

---

## Roadmap

Planned or ongoing work:

- improve input and mouse focus behavior
- test additional Data Center game updates
- improve installation documentation
- verify release ZIP structure
- document known issues more clearly
- improve compatibility notes for MelonLoader users

Project board:

[Unity Explorer – Data Center Roadmap](https://github.com/users/BloddyMichi/projects/1)

---

## Disclaimer

This is a community-maintained fork and is not an official tool from the Data Center developers.

Use this tool at your own risk.
It is intended for debugging, modding, testing and development purposes.

Do not use runtime inspection or modding tools in environments where they are not allowed.

---

## License

This project is licensed under the **GPL-3.0 License**.

See:

[LICENSE](./LICENSE)

---

## Credits

Based on UnityExplorer and related Unity modding tools.

Maintained by:

**Bloody / BloddyMichi**
