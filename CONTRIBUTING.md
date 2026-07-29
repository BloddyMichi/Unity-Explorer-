# Contributing / Mitwirken

Thanks for your interest in this Data Center-focused UnityExplorer fork!
This document is bilingual: **English first, Deutsch darunter.**

---

## English

### Scope of this fork

This repository is a compatibility fork of UnityExplorer, specialized for the
Steam game **Data Center** (Unity 6000, IL2CPP, MelonLoader CoreCLR / net6).
Changes should keep this target working. Features that only make sense for other
games or loaders are welcome only if they do not destabilize the Data Center path.

### Branching

- The default branch is `main`.
- Do **not** commit directly to `main`. Create a feature branch, e.g.
  `feature/short-description` or `fix/short-description`, and open a pull request.
- Keep one logical change per pull request where possible.

### Building

Prerequisites: Windows, PowerShell, and the .NET SDK. The SDK version is pinned
in [`global.json`](global.json); install a matching 8.0.x SDK.

- **Data Center target only** (fastest, what most users need):

  ```powershell
  .\build-datacenter.ps1
  ```

  This builds the `ML_Cpp_CoreCLR` configuration and produces
  `Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip` plus
  `Release\CHECKSUMS_SHA256.txt`.

- **All configurations** (full upstream build matrix):

  ```powershell
  .\build.ps1
  ```

- **Install into a local Data Center install** for testing:

  ```powershell
  .\install-datacenter.ps1 -BuildIfMissing
  ```

### Tests

Platform-independent logic is covered by the test project in
[`tests/`](tests). Run:

```powershell
dotnet test tests/UnityExplorer.Tests/UnityExplorer.Tests.csproj
```

UI/runtime behavior that depends on Unity or IL2CPP cannot be unit-tested and
must be verified in-game (see below).

### Code style

- Style is enforced by [`src/.editorconfig`](src/.editorconfig) and the
  `Microsoft.Unity.Analyzers` package. Please keep new code warning-clean.
- Match the surrounding code: naming, brace style, and the existing
  `#if CPP / MONO / INTEROP / UNHOLLOWER` conditional-compilation patterns.

### Verifying a change in-game

Dependency and runtime changes **must** be smoke-tested against Data Center
before merging:

1. Build with `build-datacenter.ps1` and install the resulting mod.
2. Launch the game and open the UnityExplorer UI.
3. Confirm the Object Explorer lists objects, the C# console evaluates an
   expression, the Inspector opens, and a hook can be created.

### Pull request expectations

- Describe what changed and why, and note whether you tested in-game.
- Update [`CHANGELOG.md`](CHANGELOG.md) under `## Unreleased` (bilingual entries).
- Bump versions only when releasing (see the version scheme note in `CHANGELOG.md`).

---

## Deutsch

### Ziel dieses Forks

Dieses Repository ist ein Kompatibilitäts-Fork von UnityExplorer, spezialisiert
auf das Steam-Spiel **Data Center** (Unity 6000, IL2CPP, MelonLoader CoreCLR /
net6). Änderungen sollen dieses Ziel lauffähig halten. Funktionen, die nur für
andere Spiele oder Loader sinnvoll sind, sind nur willkommen, wenn sie den
Data-Center-Pfad nicht destabilisieren.

### Branches

- Der Standard-Branch ist `main`.
- **Nicht** direkt auf `main` committen. Lege einen Feature-Branch an, z. B.
  `feature/kurzbeschreibung` oder `fix/kurzbeschreibung`, und öffne einen Pull Request.
- Möglichst eine logische Änderung pro Pull Request.

### Bauen

Voraussetzungen: Windows, PowerShell und das .NET SDK. Die SDK-Version ist in
[`global.json`](global.json) gepinnt; installiere ein passendes 8.0.x-SDK.

- **Nur Data-Center-Ziel** (am schnellsten, für die meisten Nutzer ausreichend):

  ```powershell
  .\build-datacenter.ps1
  ```

  Baut die Konfiguration `ML_Cpp_CoreCLR` und erzeugt
  `Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip` plus
  `Release\CHECKSUMS_SHA256.txt`.

- **Alle Konfigurationen** (vollständige Upstream-Build-Matrix):

  ```powershell
  .\build.ps1
  ```

- **In eine lokale Data-Center-Installation installieren** (zum Testen):

  ```powershell
  .\install-datacenter.ps1 -BuildIfMissing
  ```

### Tests

Plattformunabhängige Logik wird durch das Testprojekt in [`tests/`](tests)
abgedeckt. Ausführen:

```powershell
dotnet test tests/UnityExplorer.Tests/UnityExplorer.Tests.csproj
```

UI-/Laufzeitverhalten, das von Unity oder IL2CPP abhängt, lässt sich nicht per
Unit-Test prüfen und muss im Spiel verifiziert werden (siehe unten).

### Code-Stil

- Der Stil wird durch [`src/.editorconfig`](src/.editorconfig) und das Paket
  `Microsoft.Unity.Analyzers` erzwungen. Bitte neuen Code warnungsfrei halten.
- Am umgebenden Code orientieren: Benennung, Klammer-Stil und die bestehenden
  `#if CPP / MONO / INTEROP / UNHOLLOWER`-Muster der bedingten Kompilierung.

### Änderung im Spiel verifizieren

Dependency- und Laufzeitänderungen **müssen** vor dem Merge gegen Data Center
smoke-getestet werden:

1. Mit `build-datacenter.ps1` bauen und das resultierende Mod installieren.
2. Spiel starten und die UnityExplorer-UI öffnen.
3. Prüfen, dass der Object Explorer Objekte listet, die C#-Konsole einen
   Ausdruck auswertet, der Inspector öffnet und ein Hook erstellt werden kann.

### Erwartungen an Pull Requests

- Beschreibe, was sich geändert hat und warum, und ob du im Spiel getestet hast.
- Aktualisiere [`CHANGELOG.md`](CHANGELOG.md) unter `## Unreleased` (zweisprachige Einträge).
- Versionen nur beim Release erhöhen (siehe Versionsschema-Hinweis in `CHANGELOG.md`).
