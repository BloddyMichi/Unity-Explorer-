param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Data Center",
    [switch]$BuildIfMissing,
    [switch]$SkipBackup,
    [switch]$SkipProcessCheck,
    [switch]$WaitForGameExit,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$BuildScript = Join-Path $PSScriptRoot "build-datacenter.ps1"
$BuildDir = Join-Path $PSScriptRoot "Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR"
$ModDll = Join-Path $BuildDir "Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll"
$UserLib = Join-Path $BuildDir "UserLibs\UniverseLib.ML.IL2CPP.Interop.dll"

if (!(Test-Path $ModDll) -or !(Test-Path $UserLib)) {
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

if (!(Test-Path $ModDll) -or !(Test-Path $UserLib)) {
    throw "Build output is still missing after build step."
}

$ResolvedGameDir = Resolve-Path -LiteralPath $GameDir -ErrorAction SilentlyContinue
if ($ResolvedGameDir) {
    $GameDir = $ResolvedGameDir.Path
}
else {
    throw "Game directory not found: $GameDir. Pass -GameDir with the correct Data Center install path."
}

$ModsDir = Join-Path $GameDir "Mods"
$UserLibsDir = Join-Path $GameDir "UserLibs"
$BackupDir = Join-Path $GameDir ("UnityExplorer.Backups\" + (Get-Date -Format "yyyyMMdd-HHmmss"))
$DidBackup = $false

function Backup-ExistingFile {
    param([string]$Path)

    if ($SkipBackup -or !(Test-Path $Path)) {
        return
    }

    if (!$script:DidBackup) {
        New-Item -ItemType Directory -Force -Path $script:BackupDir | Out-Null
        $script:DidBackup = $true
    }

    Copy-Item -LiteralPath $Path -Destination (Join-Path $script:BackupDir (Split-Path $Path -Leaf)) -Force
}

function Write-InstalledFile {
    param([string]$Path)

    $Hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
    Write-Host "  $Path"
    Write-Host "    SHA256 $Hash"
}

function Write-PlannedFile {
    param(
        [string]$Source,
        [string]$Destination
    )

    $Hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Source).Hash.ToLowerInvariant()
    Write-Host "  $Source"
    Write-Host "    -> $Destination"
    Write-Host "    SHA256 $Hash"
}

function Get-DataCenterProcesses {
    $Processes = @()

    foreach ($Process in Get-Process) {
        $ProcessPath = $null
        try {
            $ProcessPath = $Process.Path
        }
        catch {
            $ProcessPath = $null
        }

        if ($ProcessPath -and $ProcessPath.StartsWith($GameDir, [System.StringComparison]::OrdinalIgnoreCase)) {
            $Processes += $Process
        }
    }

    return $Processes
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
    $MelonLoaderDir = Join-Path $GameDir "MelonLoader"
    if (!(Test-Path -LiteralPath $MelonLoaderDir)) {
        throw "MelonLoader was not found in this game directory: $MelonLoaderDir`nInstall MelonLoader for Data Center first, then rerun this script."
    }

    $MelonLoaderDll = Join-Path $MelonLoaderDir "net6\MelonLoader.dll"
    if (!(Test-Path -LiteralPath $MelonLoaderDll)) {
        Write-Host "MelonLoader folder found, but net6\MelonLoader.dll was not detected. This may be a different or incomplete MelonLoader install." -ForegroundColor Yellow
    }
}

function Write-DuplicateWarnings {
    $KnownDuplicateFiles = @(
        (Join-Path $ModsDir "UniverseLib.ML.IL2CPP.Interop.dll"),
        (Join-Path $ModsDir "UniverseLib.ML.IL2CPP.dll"),
        (Join-Path $ModsDir "UnityExplorer.ML.IL2CPP.dll"),
        (Join-Path $UserLibsDir "UnityExplorer.ML.IL2CPP.CoreCLR.dll")
    )

    $FoundAny = $false
    foreach ($Path in $KnownDuplicateFiles) {
        if (Test-Path -LiteralPath $Path) {
            if (!$FoundAny) {
                Write-Host "Potential old or misplaced UnityExplorer files found:" -ForegroundColor Yellow
                $FoundAny = $true
            }

            Write-Host "  $Path"
        }
    }

    if ($FoundAny) {
        Write-Host "Consider removing these files if UnityExplorer does not load correctly." -ForegroundColor Yellow
    }
}

$InstalledModDll = Join-Path $ModsDir "UnityExplorer.ML.IL2CPP.CoreCLR.dll"
$InstalledUserLib = Join-Path $UserLibsDir "UniverseLib.ML.IL2CPP.Interop.dll"

if (!$SkipProcessCheck) {
    $RunningGameProcesses = Get-DataCenterProcesses
    if ($RunningGameProcesses.Count -gt 0) {
        if ($WaitForGameExit) {
            Write-Host "Waiting for Data Center to exit before installing..." -ForegroundColor Yellow
            while ($RunningGameProcesses.Count -gt 0) {
                foreach ($Process in $RunningGameProcesses) {
                    Write-Host ("  still running: {0} (PID {1})" -f $Process.ProcessName, $Process.Id)
                }

                Start-Sleep -Seconds 2
                $RunningGameProcesses = Get-DataCenterProcesses
            }
        }
        else {
            Write-Host "Data Center appears to be running from this game directory:" -ForegroundColor Yellow
            foreach ($Process in $RunningGameProcesses) {
                Write-Host ("  {0} (PID {1})" -f $Process.ProcessName, $Process.Id)
            }

            throw "Close Data Center before installing UnityExplorer, or rerun with -WaitForGameExit."
        }
    }
}

Assert-MelonLoaderInstall

New-Item -ItemType Directory -Force -Path $ModsDir | Out-Null
New-Item -ItemType Directory -Force -Path $UserLibsDir | Out-Null

Write-DuplicateWarnings

Assert-FileIsReplaceable $InstalledModDll
Assert-FileIsReplaceable $InstalledUserLib

if ($DryRun) {
    Write-Host "Dry run only. No files were copied." -ForegroundColor Cyan
    Write-Host "Planned install target:" -ForegroundColor Cyan
    Write-Host $GameDir
    Write-Host "Planned files:" -ForegroundColor Cyan
    Write-PlannedFile $ModDll $InstalledModDll
    Write-PlannedFile $UserLib $InstalledUserLib
    exit 0
}

Backup-ExistingFile $InstalledModDll
Backup-ExistingFile $InstalledUserLib

Copy-Item -LiteralPath $ModDll -Destination $InstalledModDll -Force
Copy-Item -LiteralPath $UserLib -Destination $InstalledUserLib -Force

Write-Host "Installed Data Center UnityExplorer Safe Fork to:" -ForegroundColor Green
Write-Host $GameDir

if ($DidBackup) {
    Write-Host "Previous files were backed up to:" -ForegroundColor Yellow
    Write-Host $BackupDir
}

Write-Host "Installed files:" -ForegroundColor Green
Write-InstalledFile $InstalledModDll
Write-InstalledFile $InstalledUserLib
