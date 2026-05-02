$ErrorActionPreference = "Stop"

Write-Host "== UnityExplorer Data Center CoreCLR Build ==" -ForegroundColor Cyan

$BuildDir = Join-Path $PSScriptRoot "Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR"
$ModsDir = Join-Path $BuildDir "Mods"
$UserLibsDir = Join-Path $BuildDir "UserLibs"
$ModDllName = "UnityExplorer.ML.IL2CPP.CoreCLR.dll"
$UserLibName = "UniverseLib.ML.IL2CPP.Interop.dll"
$RootModDll = Join-Path $BuildDir $ModDllName
$RootUserLib = Join-Path $BuildDir $UserLibName
$McsDll = Join-Path $BuildDir "mcs.dll"
$ModDll = Join-Path $ModsDir $ModDllName
$UserLib = Join-Path $UserLibsDir $UserLibName
$ZipPath = Join-Path $PSScriptRoot "Release\UnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip"

function Assert-LastCommandSucceeded {
    param([string]$StepName)

    if ($LASTEXITCODE -ne 0) {
        throw "$StepName failed with exit code $LASTEXITCODE."
    }
}

Push-Location $PSScriptRoot
try {
    Write-Host "Building UniverseLib IL2CPP Interop for MelonLoader..." -ForegroundColor Cyan
    & dotnet build "UniverseLib\src\UniverseLib.sln" -c Release_IL2CPP_Interop_ML
    Assert-LastCommandSucceeded "UniverseLib build"

    Write-Host "Building UnityExplorer MelonLoader IL2CPP CoreCLR..." -ForegroundColor Cyan
    & dotnet build "src\UnityExplorer.sln" -c Release_ML_Cpp_CoreCLR
    Assert-LastCommandSucceeded "UnityExplorer build"

    Write-Host "Merging mcs.dll into UnityExplorer assembly..." -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "lib\ILRepack.exe") `
        /target:library `
        /lib:"lib\net6" `
        /lib:"lib\interop" `
        /lib:$BuildDir `
        /internalize `
        /out:$RootModDll `
        $RootModDll `
        $McsDll
    Assert-LastCommandSucceeded "ILRepack"

    Remove-Item (Join-Path $BuildDir "UnityExplorer.ML.IL2CPP.CoreCLR.deps.json") -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $BuildDir "UnityExplorer.ML.IL2CPP.CoreCLR.pdb") -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $BuildDir "Tomlet.dll") -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $BuildDir "mcs.dll") -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $BuildDir "Iced.dll") -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $BuildDir "Il2CppInterop.Common.dll") -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $BuildDir "Il2CppInterop.Runtime.dll") -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $BuildDir "Microsoft.Extensions.Logging.Abstractions.dll") -ErrorAction SilentlyContinue

    New-Item -ItemType Directory -Force -Path $ModsDir | Out-Null
    New-Item -ItemType Directory -Force -Path $UserLibsDir | Out-Null

    Move-Item -Path $RootModDll -Destination $ModDll -Force
    Move-Item -Path $RootUserLib -Destination $UserLib -Force

    Remove-Item $ZipPath -ErrorAction SilentlyContinue
    Compress-Archive -Path (Join-Path $BuildDir "*") -DestinationPath $ZipPath -Force
}
finally {
    Pop-Location
}

if ((Test-Path $ModDll) -and (Test-Path $UserLib)) {
    Write-Host ""
    Write-Host "Data Center CoreCLR build is available:" -ForegroundColor Green
    Write-Host $ModDll
    Write-Host $UserLib
    Write-Host $ZipPath
    exit 0
}

Write-Host ""
Write-Host "Data Center CoreCLR build output was not found." -ForegroundColor Red
exit 1
