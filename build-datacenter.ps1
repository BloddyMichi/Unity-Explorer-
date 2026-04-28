$ErrorActionPreference = "Continue"

Write-Host "== UnityExplorer Data Center CoreCLR Build ==" -ForegroundColor Cyan
Write-Host "Running upstream build.ps1. Mono/net35 target errors can be ignored for Data Center." -ForegroundColor Yellow

powershell -ExecutionPolicy Bypass -File "$PSScriptRoot\build.ps1"

$BuildDir = Join-Path $PSScriptRoot "Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR"
$ModDll = Join-Path $BuildDir "Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll"
$UserLib = Join-Path $BuildDir "UserLibs\UniverseLib.ML.IL2CPP.Interop.dll"

if ((Test-Path $ModDll) -and (Test-Path $UserLib)) {
    Write-Host ""
    Write-Host "Data Center CoreCLR build is available:" -ForegroundColor Green
    Write-Host $ModDll
    Write-Host $UserLib
    exit 0
}

Write-Host ""
Write-Host "Data Center CoreCLR build output was not found." -ForegroundColor Red
exit 1
