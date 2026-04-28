param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Data Center"
)

$ErrorActionPreference = "Stop"

$BuildDir = Join-Path $PSScriptRoot "Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR"
$ModDll = Join-Path $BuildDir "Mods\UnityExplorer.ML.IL2CPP.CoreCLR.dll"
$UserLib = Join-Path $BuildDir "UserLibs\UniverseLib.ML.IL2CPP.Interop.dll"

if (!(Test-Path $ModDll) -or !(Test-Path $UserLib)) {
    throw "Build output not found. Run .\build-datacenter.ps1 first."
}

$ModsDir = Join-Path $GameDir "Mods"
$UserLibsDir = Join-Path $GameDir "UserLibs"

New-Item -ItemType Directory -Force -Path $ModsDir | Out-Null
New-Item -ItemType Directory -Force -Path $UserLibsDir | Out-Null

Copy-Item $ModDll (Join-Path $ModsDir "UnityExplorer.ML.IL2CPP.CoreCLR.dll") -Force
Copy-Item $UserLib (Join-Path $UserLibsDir "UniverseLib.ML.IL2CPP.Interop.dll") -Force

Write-Host "Installed Data Center UnityExplorer Safe Fork to:" -ForegroundColor Green
Write-Host $GameDir
