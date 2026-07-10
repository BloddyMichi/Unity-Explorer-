# Data Center Improvements TDD Evidence

## Source

Journeys were derived from the requested 1-to-6 improvement package:

- Installer comfort features.
- In-game diagnostics.
- Release automation.
- Configurable Unity 6000 safe mode.
- Expanded Object Search.
- Generated known-issues documentation.

## RED Evidence

Command:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\datacenter-tooling.tests.ps1
```

Initial result:

```text
FAIL: installer supports auto-detect helper did not contain pattern: Find-DataCenterGameDir
```

This confirmed that the expected installer/status/release-documentation capabilities did not exist before implementation.

## GREEN Evidence

Command:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\datacenter-tooling.tests.ps1
```

Final result:

```text
All Data Center tooling tests passed.
```

## Test Specification

| # | Guarantee | Test file or command | Type | Result |
|---|---|---|---|---|
| 1 | Installer exposes auto-detect, status, uninstall, restore-backup and open-log modes. | `tests/datacenter-tooling.tests.ps1` | script contract | PASS |
| 2 | Release workflow guards existing releases/tags, validates ZIP contents and reads `CHANGELOG.md`. | `tests/datacenter-tooling.tests.ps1` | workflow contract | PASS |
| 3 | README documents uninstall, restore-backup and support-package commands. | `tests/datacenter-tooling.tests.ps1` | docs contract | PASS |
| 4 | Troubleshooting docs mention status checks and duplicate file handling. | `tests/datacenter-tooling.tests.ps1` | docs contract | PASS |
| 5 | Data Center CoreCLR build compiles after runtime changes. | `powershell -ExecutionPolicy Bypass -File .\build-datacenter.ps1` | build | PASS |

## Coverage And Gaps

This repository does not currently include a C# unit-test project or an automated Unity runtime harness. The PowerShell contract tests cover the script and release workflow surface. The C# Unity UI changes are covered by the Data Center CoreCLR build and require manual in-game verification after installing the produced DLLs.
