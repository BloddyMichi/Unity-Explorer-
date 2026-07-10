param(
    [string]$GameDir,
    [switch]$BuildIfMissing,
    [switch]$SkipBackup,
    [switch]$SkipProcessCheck,
    [switch]$WaitForGameExit,
    [switch]$DryRun,
    [switch]$Status,
    [switch]$Uninstall,
    [string]$RestoreBackup,
    [switch]$OpenLogs
)

$ErrorActionPreference = "Stop"

$BuildScript = Join-Path $PSScriptRoot "build-datacenter.ps1"
$BuildDir = Join-Path $PSScriptRoot "Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR"
$ModDllName = "UnityExplorer.ML.IL2CPP.CoreCLR.dll"
$UserLibName = "UniverseLib.ML.IL2CPP.Interop.dll"
$ModDll = Join-Path $BuildDir "Mods\$ModDllName"
$UserLib = Join-Path $BuildDir "UserLibs\$UserLibName"

function Add-UniquePath {
    param(
        [System.Collections.Generic.List[string]]$Paths,
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $Normalized = $Path.Trim().Trim('"') -replace '/', '\'
    if (!$Paths.Contains($Normalized)) {
        $Paths.Add($Normalized)
    }
}

function Get-SteamInstallRoots {
    $Paths = [System.Collections.Generic.List[string]]::new()

    Add-UniquePath $Paths "C:\Program Files (x86)\Steam"
    Add-UniquePath $Paths "C:\Program Files\Steam"

    foreach ($RegistryPath in @(
        "HKCU:\Software\Valve\Steam",
        "HKLM:\SOFTWARE\Valve\Steam",
        "HKLM:\SOFTWARE\Wow6432Node\Valve\Steam"
    )) {
        try {
            $SteamKey = Get-ItemProperty -LiteralPath $RegistryPath -ErrorAction Stop
            Add-UniquePath $Paths $SteamKey.SteamPath
            Add-UniquePath $Paths $SteamKey.InstallPath
        }
        catch {
        }
    }

    return $Paths
}

function Get-SteamLibraryRoots {
    $Libraries = [System.Collections.Generic.List[string]]::new()

    foreach ($SteamRoot in Get-SteamInstallRoots) {
        Add-UniquePath $Libraries $SteamRoot

        $LibraryFile = Join-Path $SteamRoot "steamapps\libraryfolders.vdf"
        if (!(Test-Path -LiteralPath $LibraryFile)) {
            continue
        }

        foreach ($Line in Get-Content -LiteralPath $LibraryFile) {
            if ($Line -match '"path"\s+"(?<path>.+)"') {
                Add-UniquePath $Libraries ($Matches.path -replace '\\\\', '\')
            }
        }
    }

    foreach ($Drive in Get-PSDrive -PSProvider FileSystem) {
        Add-UniquePath $Libraries (Join-Path $Drive.Root "SteamLibrary")
        Add-UniquePath $Libraries (Join-Path $Drive.Root "Steam")
    }

    return $Libraries
}

function Find-DataCenterGameDir {
    $Candidates = [System.Collections.Generic.List[string]]::new()
    Add-UniquePath $Candidates "C:\Program Files (x86)\Steam\steamapps\common\Data Center"

    foreach ($LibraryRoot in Get-SteamLibraryRoots) {
        Add-UniquePath $Candidates (Join-Path $LibraryRoot "steamapps\common\Data Center")
    }

    foreach ($Candidate in $Candidates) {
        if (Test-Path -LiteralPath $Candidate) {
            return (Resolve-Path -LiteralPath $Candidate).Path
        }
    }

    return $null
}

function Resolve-DataCenterGameDir {
    param([string]$RequestedGameDir)

    if (![string]::IsNullOrWhiteSpace($RequestedGameDir)) {
        $Resolved = Resolve-Path -LiteralPath $RequestedGameDir -ErrorAction SilentlyContinue
        if ($Resolved) {
            return $Resolved.Path
        }

        throw "Game directory not found: $RequestedGameDir. Pass -GameDir with the correct Data Center install path."
    }

    $Detected = Find-DataCenterGameDir
    if ($Detected) {
        Write-Host "Auto-detected Data Center directory:" -ForegroundColor Cyan
        Write-Host $Detected
        return $Detected
    }

    throw "Could not auto-detect Data Center. Pass -GameDir with the correct install path."
}

function Get-InstalledPaths {
    param([string]$ResolvedGameDir)

    $ModsDir = Join-Path $ResolvedGameDir "Mods"
    $UserLibsDir = Join-Path $ResolvedGameDir "UserLibs"

    [pscustomobject]@{
        ModsDir = $ModsDir
        UserLibsDir = $UserLibsDir
        ModDll = Join-Path $ModsDir $ModDllName
        UserLib = Join-Path $UserLibsDir $UserLibName
        BackupRoot = Join-Path $ResolvedGameDir "UnityExplorer.Backups"
        MelonLoaderDir = Join-Path $ResolvedGameDir "MelonLoader"
        MelonLoaderDll = Join-Path $ResolvedGameDir "MelonLoader\net6\MelonLoader.dll"
        MelonLog = Join-Path $ResolvedGameDir "MelonLoader\Latest.log"
        UnityExplorerLogDir = Join-Path $ResolvedGameDir "Mods\sinai-dev-UnityExplorer\Logs"
    }
}

function Get-HashOrStatus {
    param([string]$Path)

    if (!(Test-Path -LiteralPath $Path)) {
        return "missing"
    }

    try {
        return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
    }
    catch {
        return "locked_or_unreadable"
    }
}

function Get-DataCenterProcesses {
    param([string]$ResolvedGameDir)

    $Processes = @()

    foreach ($Process in Get-Process) {
        $ProcessPath = $null
        try {
            $ProcessPath = $Process.Path
        }
        catch {
            $ProcessPath = $null
        }

        if ($ProcessPath -and $ProcessPath.StartsWith($ResolvedGameDir, [System.StringComparison]::OrdinalIgnoreCase)) {
            $Processes += $Process
        }
    }

    return $Processes
}

function Get-DuplicateFiles {
    param([pscustomobject]$Paths)

    @(
        (Join-Path $Paths.ModsDir "UniverseLib.ML.IL2CPP.Interop.dll"),
        (Join-Path $Paths.ModsDir "UniverseLib.ML.IL2CPP.dll"),
        (Join-Path $Paths.ModsDir "UnityExplorer.ML.IL2CPP.dll"),
        (Join-Path $Paths.UserLibsDir "UnityExplorer.ML.IL2CPP.CoreCLR.dll")
    ) | Where-Object { Test-Path -LiteralPath $_ }
}

function Get-LatestBackup {
    param([pscustomobject]$Paths)

    if (!(Test-Path -LiteralPath $Paths.BackupRoot)) {
        return $null
    }

    Get-ChildItem -LiteralPath $Paths.BackupRoot -Directory |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Get-InstallState {
    param(
        [string]$ResolvedGameDir,
        [pscustomobject]$Paths
    )

    $BuildModHash = if (Test-Path -LiteralPath $ModDll) { Get-HashOrStatus $ModDll } else { "build_missing" }
    $BuildUserLibHash = if (Test-Path -LiteralPath $UserLib) { Get-HashOrStatus $UserLib } else { "build_missing" }
    $InstalledModHash = Get-HashOrStatus $Paths.ModDll
    $InstalledUserLibHash = Get-HashOrStatus $Paths.UserLib
    $RunningProcesses = @(Get-DataCenterProcesses $ResolvedGameDir)
    $Duplicates = @(Get-DuplicateFiles $Paths)
    $LatestBackup = Get-LatestBackup $Paths

    $ModState = if ($InstalledModHash -eq "missing") {
        "missing"
    }
    elseif ($BuildModHash -ne "build_missing" -and $InstalledModHash -eq $BuildModHash) {
        "current"
    }
    else {
        "installed_different_or_older"
    }

    $UserLibState = if ($InstalledUserLibHash -eq "missing") {
        "missing"
    }
    elseif ($BuildUserLibHash -ne "build_missing" -and $InstalledUserLibHash -eq $BuildUserLibHash) {
        "current"
    }
    else {
        "installed_different_or_older"
    }

    [pscustomobject]@{
        GameDir = $ResolvedGameDir
        ModState = $ModState
        UserLibState = $UserLibState
        InstalledModHash = $InstalledModHash
        InstalledUserLibHash = $InstalledUserLibHash
        BuildModHash = $BuildModHash
        BuildUserLibHash = $BuildUserLibHash
        MelonLoader = if (Test-Path -LiteralPath $Paths.MelonLoaderDir) { "found" } else { "missing" }
        MelonLoaderNet6 = if (Test-Path -LiteralPath $Paths.MelonLoaderDll) { "found" } else { "missing" }
        RunningProcesses = $RunningProcesses
        DuplicateFiles = $Duplicates
        LatestBackup = $LatestBackup
        LatestLog = if (Test-Path -LiteralPath $Paths.MelonLog) { $Paths.MelonLog } else { $null }
        UnityExplorerLogDir = if (Test-Path -LiteralPath $Paths.UnityExplorerLogDir) { $Paths.UnityExplorerLogDir } else { $null }
    }
}

function Write-InstallState {
    param([pscustomobject]$State)

    Write-Host "Data Center UnityExplorer install state" -ForegroundColor Cyan
    Write-Host "GameDir: $($State.GameDir)"
    Write-Host "MelonLoader: $($State.MelonLoader) (net6 dll: $($State.MelonLoaderNet6))"
    Write-Host "Mod DLL: $($State.ModState)"
    Write-Host "  installed: $($State.InstalledModHash)"
    Write-Host "  build:     $($State.BuildModHash)"
    Write-Host "UserLib DLL: $($State.UserLibState)"
    Write-Host "  installed: $($State.InstalledUserLibHash)"
    Write-Host "  build:     $($State.BuildUserLibHash)"

    if ($State.RunningProcesses.Count -gt 0) {
        Write-Host "Running Data Center processes:" -ForegroundColor Yellow
        foreach ($Process in $State.RunningProcesses) {
            Write-Host ("  {0} (PID {1})" -f $Process.ProcessName, $Process.Id)
        }
    }
    else {
        Write-Host "Running Data Center processes: none"
    }

    if ($State.DuplicateFiles.Count -gt 0) {
        Write-Host "Potential old or misplaced duplicate files:" -ForegroundColor Yellow
        foreach ($Duplicate in $State.DuplicateFiles) {
            Write-Host "  $Duplicate"
        }
    }
    else {
        Write-Host "Potential old or misplaced duplicate files: none"
    }

    if ($State.LatestBackup) {
        Write-Host "Latest backup: $($State.LatestBackup.FullName)"
    }
    else {
        Write-Host "Latest backup: none"
    }

    if ($State.LatestLog) {
        Write-Host "Latest MelonLoader log: $($State.LatestLog)"
    }

    if ($State.UnityExplorerLogDir) {
        Write-Host "UnityExplorer logs: $($State.UnityExplorerLogDir)"
    }
}

function Ensure-BuildOutput {
    if (!(Test-Path -LiteralPath $ModDll) -or !(Test-Path -LiteralPath $UserLib)) {
        if ($BuildIfMissing) {
            Write-Host "Build output not found. Running build-datacenter.ps1..." -ForegroundColor Yellow
            & powershell -ExecutionPolicy Bypass -File $BuildScript
            if ($LASTEXITCODE -ne 0) {
                throw "build-datacenter.ps1 failed with exit code $LASTEXITCODE."
            }
        }
        else {
            throw "Build output not found. Run .\build-datacenter.ps1 first, or use -BuildIfMissing."
        }
    }

    if (!(Test-Path -LiteralPath $ModDll) -or !(Test-Path -LiteralPath $UserLib)) {
        throw "Build output is still missing after build step."
    }
}

function Assert-FileIsReplaceable {
    param([string]$Path)

    if (!(Test-Path -LiteralPath $Path)) {
        return
    }

    $Stream = $null
    try {
        $Stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    }
    catch {
        throw "File is currently locked and cannot be replaced: $Path`nClose Data Center and rerun this installer."
    }
    finally {
        if ($Stream) {
            $Stream.Dispose()
        }
    }
}

function Assert-MelonLoaderInstall {
    param([pscustomobject]$Paths)

    if (!(Test-Path -LiteralPath $Paths.MelonLoaderDir)) {
        throw "MelonLoader was not found in this game directory: $($Paths.MelonLoaderDir)`nInstall MelonLoader for Data Center first, then rerun this script."
    }

    if (!(Test-Path -LiteralPath $Paths.MelonLoaderDll)) {
        Write-Host "MelonLoader folder found, but net6\MelonLoader.dll was not detected. This may be a different or incomplete MelonLoader install." -ForegroundColor Yellow
    }
}

function Wait-Or-FailIfGameIsRunning {
    param([string]$ResolvedGameDir)

    if ($SkipProcessCheck) {
        return
    }

    $RunningGameProcesses = @(Get-DataCenterProcesses $ResolvedGameDir)
    if ($RunningGameProcesses.Count -eq 0) {
        return
    }

    if ($WaitForGameExit) {
        Write-Host "Waiting for Data Center to exit before continuing..." -ForegroundColor Yellow
        while ($RunningGameProcesses.Count -gt 0) {
            foreach ($Process in $RunningGameProcesses) {
                Write-Host ("  still running: {0} (PID {1})" -f $Process.ProcessName, $Process.Id)
            }

            Start-Sleep -Seconds 2
            $RunningGameProcesses = @(Get-DataCenterProcesses $ResolvedGameDir)
        }

        return
    }

    Write-Host "Data Center appears to be running from this game directory:" -ForegroundColor Yellow
    foreach ($Process in $RunningGameProcesses) {
        Write-Host ("  {0} (PID {1})" -f $Process.ProcessName, $Process.Id)
    }

    throw "Close Data Center before changing UnityExplorer files, or rerun with -WaitForGameExit."
}

function Backup-ExistingFile {
    param(
        [string]$Path,
        [string]$BackupDir
    )

    if ($SkipBackup -or !(Test-Path -LiteralPath $Path)) {
        return $false
    }

    New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
    Copy-Item -LiteralPath $Path -Destination (Join-Path $BackupDir (Split-Path $Path -Leaf)) -Force
    return $true
}

function Write-PlannedFile {
    param(
        [string]$Source,
        [string]$Destination
    )

    $Hash = (Get-HashOrStatus $Source)
    Write-Host "  $Source"
    Write-Host "    -> $Destination"
    Write-Host "    SHA256 $Hash"
}

function Write-InstalledFile {
    param([string]$Path)

    $Hash = Get-HashOrStatus $Path
    Write-Host "  $Path"
    Write-Host "    SHA256 $Hash"
}

function Write-DuplicateWarnings {
    param([pscustomobject]$Paths)

    $DuplicateFiles = @(Get-DuplicateFiles $Paths)
    if ($DuplicateFiles.Count -eq 0) {
        return
    }

    Write-Host "Potential old or misplaced UnityExplorer duplicate files found:" -ForegroundColor Yellow
    foreach ($Path in $DuplicateFiles) {
        Write-Host "  $Path"
    }

    Write-Host "Consider removing these files if UnityExplorer does not load correctly." -ForegroundColor Yellow
}

function Invoke-OpenLogs {
    param([pscustomobject]$Paths)

    if (Test-Path -LiteralPath $Paths.MelonLog) {
        Invoke-Item -LiteralPath $Paths.MelonLog
        Write-Host "Opened MelonLoader log: $($Paths.MelonLog)" -ForegroundColor Green
    }
    else {
        Write-Host "MelonLoader Latest.log not found: $($Paths.MelonLog)" -ForegroundColor Yellow
    }

    if (Test-Path -LiteralPath $Paths.UnityExplorerLogDir) {
        Invoke-Item -LiteralPath $Paths.UnityExplorerLogDir
        Write-Host "Opened UnityExplorer log folder: $($Paths.UnityExplorerLogDir)" -ForegroundColor Green
    }
    else {
        Write-Host "UnityExplorer log folder not found: $($Paths.UnityExplorerLogDir)" -ForegroundColor Yellow
    }
}

function Resolve-BackupToRestore {
    param(
        [pscustomobject]$Paths,
        [string]$RequestedBackup
    )

    if ([string]::IsNullOrWhiteSpace($RequestedBackup) -or $RequestedBackup.Equals("latest", [System.StringComparison]::OrdinalIgnoreCase)) {
        $LatestBackup = Get-LatestBackup $Paths
        if (!$LatestBackup) {
            throw "No UnityExplorer backup folder was found under: $($Paths.BackupRoot)"
        }

        return $LatestBackup.FullName
    }

    if (Test-Path -LiteralPath $RequestedBackup) {
        return (Resolve-Path -LiteralPath $RequestedBackup).Path
    }

    $NamedBackup = Join-Path $Paths.BackupRoot $RequestedBackup
    if (Test-Path -LiteralPath $NamedBackup) {
        return (Resolve-Path -LiteralPath $NamedBackup).Path
    }

    throw "Backup not found: $RequestedBackup"
}

$GameDir = Resolve-DataCenterGameDir $GameDir
$Paths = Get-InstalledPaths $GameDir
$BackupDir = Join-Path $Paths.BackupRoot (Get-Date -Format "yyyyMMdd-HHmmss")
$NeedsBuildOutput = !$Status -and !$Uninstall -and [string]::IsNullOrWhiteSpace($RestoreBackup) -and !$OpenLogs

if ($OpenLogs) {
    Invoke-OpenLogs $Paths
    if (!$Status -and !$Uninstall -and [string]::IsNullOrWhiteSpace($RestoreBackup) -and !$DryRun) {
        exit 0
    }
}

if ($Status) {
    Write-InstallState (Get-InstallState $GameDir $Paths)
    if (!$Uninstall -and [string]::IsNullOrWhiteSpace($RestoreBackup) -and !$DryRun) {
        exit 0
    }
}

if ($NeedsBuildOutput -or $DryRun) {
    Ensure-BuildOutput
}

Wait-Or-FailIfGameIsRunning $GameDir

if ($Uninstall) {
    Assert-FileIsReplaceable $Paths.ModDll
    Assert-FileIsReplaceable $Paths.UserLib

    if ($DryRun) {
        Write-Host "Dry run only. No files were removed." -ForegroundColor Cyan
        Write-Host "Would remove:"
        Write-Host "  $($Paths.ModDll)"
        Write-Host "  $($Paths.UserLib)"
        exit 0
    }

    $DidBackup = $false
    $DidBackup = (Backup-ExistingFile $Paths.ModDll $BackupDir) -or $DidBackup
    $DidBackup = (Backup-ExistingFile $Paths.UserLib $BackupDir) -or $DidBackup

    Remove-Item -LiteralPath $Paths.ModDll -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $Paths.UserLib -ErrorAction SilentlyContinue

    Write-Host "Removed Data Center UnityExplorer Safe Fork files." -ForegroundColor Green
    if ($DidBackup) {
        Write-Host "Removed files were backed up to:" -ForegroundColor Yellow
        Write-Host $BackupDir
    }

    exit 0
}

if (![string]::IsNullOrWhiteSpace($RestoreBackup)) {
    $BackupToRestore = Resolve-BackupToRestore $Paths $RestoreBackup
    $BackupModDll = Join-Path $BackupToRestore $ModDllName
    $BackupUserLib = Join-Path $BackupToRestore $UserLibName

    if (!(Test-Path -LiteralPath $BackupModDll) -or !(Test-Path -LiteralPath $BackupUserLib)) {
        throw "Backup is incomplete. Expected $ModDllName and $UserLibName in: $BackupToRestore"
    }

    Assert-FileIsReplaceable $Paths.ModDll
    Assert-FileIsReplaceable $Paths.UserLib

    if ($DryRun) {
        Write-Host "Dry run only. No backup files were restored." -ForegroundColor Cyan
        Write-Host "Would restore:"
        Write-Host "  $BackupModDll -> $($Paths.ModDll)"
        Write-Host "  $BackupUserLib -> $($Paths.UserLib)"
        exit 0
    }

    New-Item -ItemType Directory -Force -Path $Paths.ModsDir | Out-Null
    New-Item -ItemType Directory -Force -Path $Paths.UserLibsDir | Out-Null
    Copy-Item -LiteralPath $BackupModDll -Destination $Paths.ModDll -Force
    Copy-Item -LiteralPath $BackupUserLib -Destination $Paths.UserLib -Force

    Write-Host "Restored UnityExplorer backup:" -ForegroundColor Green
    Write-Host $BackupToRestore
    Write-Host "Restored files:"
    Write-InstalledFile $Paths.ModDll
    Write-InstalledFile $Paths.UserLib
    exit 0
}

Assert-MelonLoaderInstall $Paths

New-Item -ItemType Directory -Force -Path $Paths.ModsDir | Out-Null
New-Item -ItemType Directory -Force -Path $Paths.UserLibsDir | Out-Null

Write-DuplicateWarnings $Paths
Assert-FileIsReplaceable $Paths.ModDll
Assert-FileIsReplaceable $Paths.UserLib

if ($DryRun) {
    Write-Host "Dry run only. No files were copied." -ForegroundColor Cyan
    Write-Host "Planned install target:" -ForegroundColor Cyan
    Write-Host $GameDir
    Write-Host "Planned files:" -ForegroundColor Cyan
    Write-PlannedFile $ModDll $Paths.ModDll
    Write-PlannedFile $UserLib $Paths.UserLib
    exit 0
}

$DidInstallBackup = $false
$DidInstallBackup = (Backup-ExistingFile $Paths.ModDll $BackupDir) -or $DidInstallBackup
$DidInstallBackup = (Backup-ExistingFile $Paths.UserLib $BackupDir) -or $DidInstallBackup

Copy-Item -LiteralPath $ModDll -Destination $Paths.ModDll -Force
Copy-Item -LiteralPath $UserLib -Destination $Paths.UserLib -Force

Write-Host "Installed Data Center UnityExplorer Safe Fork to:" -ForegroundColor Green
Write-Host $GameDir

if ($DidInstallBackup) {
    Write-Host "Previous files were backed up to:" -ForegroundColor Yellow
    Write-Host $BackupDir
}

Write-Host "Installed files:" -ForegroundColor Green
Write-InstalledFile $Paths.ModDll
Write-InstalledFile $Paths.UserLib
