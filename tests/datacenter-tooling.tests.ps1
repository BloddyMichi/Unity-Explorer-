$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot

function Read-RepoFile {
    param([string]$Path)

    Get-Content -Raw -LiteralPath (Join-Path $RepoRoot $Path)
}

function Assert-Contains {
    param(
        [string]$Name,
        [string]$Text,
        [string]$Pattern
    )

    if ($Text -notmatch $Pattern) {
        throw "FAIL: $Name did not contain pattern: $Pattern"
    }

    Write-Host "PASS: $Name"
}

$Installer = Read-RepoFile "install-datacenter.ps1"
$Workflow = Read-RepoFile ".github\workflows\release-datacenter-coreclr.yml"
$Readme = Read-RepoFile "README.md"
$Troubleshooting = Read-RepoFile "docs\TROUBLESHOOTING_DATACENTER_DE.md"

Assert-Contains "installer supports auto-detect helper" $Installer "Find-DataCenterGameDir"
Assert-Contains "installer supports status mode" $Installer '\[switch\]\$Status'
Assert-Contains "installer supports uninstall mode" $Installer '\[switch\]\$Uninstall'
Assert-Contains "installer supports restore backup mode" $Installer '\[string\]\$RestoreBackup'
Assert-Contains "installer reports install state" $Installer "Get-InstallState"
Assert-Contains "installer can open logs" $Installer '\[switch\]\$OpenLogs'

Assert-Contains "release workflow checks existing tags" $Workflow 'gh release view \$tag'
Assert-Contains "release workflow validates zip contents" $Workflow "Expand-Archive"
Assert-Contains "release workflow reads changelog" $Workflow "CHANGELOG.md"

Assert-Contains "readme documents uninstall" $Readme "-Uninstall"
Assert-Contains "readme documents restore backup" $Readme "-RestoreBackup"
Assert-Contains "readme documents support package" $Readme "collect-datacenter-logs.ps1"

Assert-Contains "troubleshooting documents status command" $Troubleshooting "-Status"
Assert-Contains "troubleshooting documents duplicated files" $Troubleshooting "doppelt|duplicate"

Write-Host "All Data Center tooling tests passed."
