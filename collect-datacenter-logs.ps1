param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Data Center",
    [string]$OutputDir = (Join-Path $PSScriptRoot "SupportPackages"),
    [switch]$IncludePlayerLog
)

$ErrorActionPreference = "Stop"

$ResolvedGameDir = Resolve-Path -LiteralPath $GameDir -ErrorAction SilentlyContinue
if ($ResolvedGameDir) {
    $GameDir = $ResolvedGameDir.Path
}
else {
    throw "Game directory not found: $GameDir. Pass -GameDir with the correct Data Center install path."
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$PackageRoot = Join-Path $OutputDir "DataCenter-UnityExplorer-Support-$Timestamp"
$ZipPath = "$PackageRoot.zip"

if (Test-Path -LiteralPath $PackageRoot) {
    Remove-Item -LiteralPath $PackageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $PackageRoot | Out-Null

function Copy-IfExists {
    param(
        [string]$Source,
        [string]$DestinationDir
    )

    if (!(Test-Path -LiteralPath $Source)) {
        return $false
    }

    New-Item -ItemType Directory -Force -Path $DestinationDir | Out-Null
    Copy-Item -LiteralPath $Source -Destination (Join-Path $DestinationDir (Split-Path $Source -Leaf)) -Force
    return $true
}

function Get-FileHashLine {
    param([string]$Path)

    if (!(Test-Path -LiteralPath $Path)) {
        return "missing  $Path"
    }

    try {
        $Hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
        return "$Hash  $Path"
    }
    catch {
        return "locked_or_unreadable  $Path"
    }
}

function Select-LogValue {
    param(
        [string]$LogPath,
        [string]$Pattern
    )

    if (!(Test-Path -LiteralPath $LogPath)) {
        return "<log missing>"
    }

    $Match = Select-String -LiteralPath $LogPath -Pattern $Pattern | Select-Object -Last 1
    if ($Match) {
        return $Match.Line.Trim()
    }

    return "<not found>"
}

$MelonLog = Join-Path $GameDir "MelonLoader\Latest.log"
$UnityExplorerLogDir = Join-Path $GameDir "Mods\sinai-dev-UnityExplorer\Logs"
$PlayerLog = Join-Path $env:USERPROFILE "AppData\LocalLow\Waseku\Data Center\Player.log"
$InstalledModDll = Join-Path $GameDir "Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll"
$InstalledUserLib = Join-Path $GameDir "UserLibs\UniverseLib.ML.IL2CPP.Interop.dll"

Copy-IfExists $MelonLog (Join-Path $PackageRoot "MelonLoader") | Out-Null

if (Test-Path -LiteralPath $UnityExplorerLogDir) {
    $TargetLogDir = Join-Path $PackageRoot "UnityExplorerLogs"
    New-Item -ItemType Directory -Force -Path $TargetLogDir | Out-Null

    Get-ChildItem -LiteralPath $UnityExplorerLogDir -Filter "*.txt" -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 10 |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $TargetLogDir $_.Name) -Force
        }
}

if ($IncludePlayerLog) {
    Copy-IfExists $PlayerLog (Join-Path $PackageRoot "Unity") | Out-Null
}

$Processes = foreach ($Process in Get-Process) {
    $ProcessPath = $null
    try {
        $ProcessPath = $Process.Path
    }
    catch {
        $ProcessPath = $null
    }

    if ($ProcessPath -and $ProcessPath.StartsWith($GameDir, [System.StringComparison]::OrdinalIgnoreCase)) {
        "{0} (PID {1}) {2}" -f $Process.ProcessName, $Process.Id, $ProcessPath
    }
}

$SupportInfo = @(
    "UnityExplorer Data Center support package",
    "Created: $(Get-Date -Format o)",
    "GameDir: $GameDir",
    "",
    "Detected versions from Latest.log:",
    "  $(Select-LogValue $MelonLog 'MelonLoader v')",
    "  $(Select-LogValue $MelonLog 'Unity Version:')",
    "  $(Select-LogValue $MelonLog 'Game Version:')",
    "",
    "Installed file hashes:",
    "  $(Get-FileHashLine $InstalledModDll)",
    "  $(Get-FileHashLine $InstalledUserLib)",
    "",
    "Running Data Center processes:",
    ($(if ($Processes) { $Processes | ForEach-Object { "  $_" } } else { "  <none>" })),
    "",
    "Important paths:",
    "  MelonLoader log: $MelonLog",
    "  UnityExplorer logs: $UnityExplorerLogDir",
    "  Player log: $PlayerLog",
    "",
    "Notes:",
    "  Player.log is only included when -IncludePlayerLog is used."
)

Set-Content -LiteralPath (Join-Path $PackageRoot "support-info.txt") -Value $SupportInfo -Encoding utf8

if (Test-Path -LiteralPath $ZipPath) {
    Remove-Item -LiteralPath $ZipPath -Force
}

Compress-Archive -Path (Join-Path $PackageRoot "*") -DestinationPath $ZipPath -Force
Remove-Item -LiteralPath $PackageRoot -Recurse -Force

Write-Host "Created Data Center support package:" -ForegroundColor Green
Write-Host $ZipPath
