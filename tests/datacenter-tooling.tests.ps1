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

function Assert-FileExists {
    param(
        [string]$Name,
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $Path))) {
        throw "FAIL: $Name file did not exist: $Path"
    }

    Write-Host "PASS: $Name"
}

$Installer = Read-RepoFile "install-datacenter.ps1"
$Workflow = Read-RepoFile ".github\workflows\release-datacenter-coreclr.yml"
$Readme = Read-RepoFile "README.md"
$Troubleshooting = Read-RepoFile "docs\TROUBLESHOOTING_DATACENTER_DE.md"
$ScrollPool = Read-RepoFile "UniverseLib\src\UI\Widgets\ScrollView\ScrollPool.cs"
$UIManager = Read-RepoFile "src\UI\UIManager.cs"
$ConfigManager = Read-RepoFile "src\Config\ConfigManager.cs"
$OptionsPanel = Read-RepoFile "src\UI\Panels\OptionsPanel.cs"
$WorldInspector = Read-RepoFile "src\Inspectors\MouseInspectors\WorldInspector.cs"

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
Assert-Contains "scroll pool guards empty data source" $ScrollPool "DataSource\.ItemCount <= 0"
Assert-Contains "deferred SetPanelActive reports missing panel creation failure" $UIManager "SetPanelActive could not create deferred panel"
Assert-Contains "config exposes world hover target label" $ConfigManager "World_Hover_Label"
Assert-Contains "options exposes hover target toggle" $OptionsPanel "Show target name while aiming"
Assert-Contains "ui manager initializes world hover label" $UIManager "WorldHoverLabel\.Init\(UIRoot\)"
Assert-Contains "ui manager updates world hover label" $UIManager "WorldHoverLabel\.Update\(\)"
Assert-Contains "world inspector exposes aim target lookup" $WorldInspector "TryFindAimTarget"

Assert-FileExists "world hover label source exists" "src\UI\WorldHoverLabel.cs"
$WorldHoverLabel = Read-RepoFile "src\UI\WorldHoverLabel.cs"
Assert-Contains "world hover label uses aim target lookup" $WorldHoverLabel "WorldInspector\.TryFindAimTarget"
Assert-Contains "world hover label does not block raycasts" $WorldHoverLabel "blocksRaycasts = false"

Write-Host "All Data Center tooling tests passed."
