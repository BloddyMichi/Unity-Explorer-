param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Data Center",
    [string]$OutputPath = (Join-Path $PSScriptRoot "docs\DATACENTER_KNOWN_ISSUES.md")
)

$ErrorActionPreference = "Stop"

$ResolvedGameDir = Resolve-Path -LiteralPath $GameDir -ErrorAction SilentlyContinue
if ($ResolvedGameDir) {
    $GameDir = $ResolvedGameDir.Path
}

$LatestLog = Join-Path $GameDir "MelonLoader\Latest.log"

function Select-LogValue {
    param(
        [string]$Pattern,
        [string]$Fallback
    )

    if (!(Test-Path -LiteralPath $LatestLog)) {
        return $Fallback
    }

    $Match = Select-String -LiteralPath $LatestLog -Pattern $Pattern | Select-Object -Last 1
    if ($Match) {
        return $Match.Line.Trim()
    }

    return $Fallback
}

$GeneratedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"
$MelonLoaderLine = Select-LogValue "MelonLoader v" "MelonLoader: <not detected>"
$UnityLine = Select-LogValue "Unity Version:" "Unity Version: <not detected>"
$GameVersionLine = Select-LogValue "Game Version:" "Game Version: <not detected>"

$Lines = @(
    "# Data Center Known Issues",
    "",
    "Generated: $GeneratedAt",
    "",
    "## Detected Runtime",
    "",
    "- $UnityLine",
    "- $MelonLoaderLine",
    "- $GameVersionLine",
    "- Game directory: $GameDir",
    "",
    "## Support Matrix",
    "",
    "| Area | Status | Notes |",
    "|---|---|---|",
    "| Unity 6000.x / IL2CPP / CoreCLR startup | Supported | Tested with Unity 6000.4.12f1 and MelonLoader 0.7.3 Open-Beta. |",
    "| C# Console | Supported with fallback | Uses safe multi-line input on Unity 6000. |",
    "| Hook Manager | Supported with fallback | Uses safe editor input and generic argument container on Unity 6000. |",
    "| Object Search | Supported | Preferred replacement for Scene Explorer in Data Center. |",
    "| Scene Explorer | Disabled by default | Disabled because SceneManager scene access can throw AccessViolationException on Unity 6000 IL2CPP/CoreCLR. |",
    "| TimeScale widget | Disabled by default | Can be re-enabled from options if needed, but restart is recommended. |",
    "| UniverseLib UI AssetBundle | Disabled by default | Built-in UI resources are used on Unity 6000. |",
    "",
    "## Known Issues",
    "",
    '- MelonLoader can log an early `EndOfStreamException` while reading game info. If UnityExplorer initializes afterwards, this is not currently blocking.',
    "- Some Unity UIElements generic type cache warnings can appear during UniverseLib startup. They are logged as warnings and have not blocked the Data Center flow.",
    "- Layer name lookup is avoided in Unity 6000 safe paths; numeric layer labels may appear instead.",
    '- If UnityExplorer does not load, run `.\install-datacenter.ps1 -Status` and check for duplicate or misplaced DLLs.',
    "",
    "## Useful Commands",
    "",
    '```powershell',
    '.\install-datacenter.ps1 -Status',
    '.\install-datacenter.ps1 -BuildIfMissing -DryRun',
    '.\install-datacenter.ps1 -Uninstall',
    '.\install-datacenter.ps1 -RestoreBackup latest',
    '.\collect-datacenter-logs.ps1',
    '.\update-datacenter-known-issues.ps1',
    '```'
)

$OutputDir = Split-Path -Parent $OutputPath
if (![string]::IsNullOrWhiteSpace($OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

Set-Content -LiteralPath $OutputPath -Value $Lines -Encoding utf8
Write-Host "Updated Data Center known issues:"
Write-Host $OutputPath
