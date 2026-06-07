param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$NativeDir = Join-Path $Root "native"
$BuildDir = Join-Path $NativeDir "build"
$CMake = (Get-Command cmake -ErrorAction SilentlyContinue).Source

if (-not $CMake) {
    $DefaultCMake = "C:\Program Files\CMake\bin\cmake.exe"
    if (Test-Path $DefaultCMake) {
        $CMake = $DefaultCMake
    }
}

if (-not $CMake) {
    throw "CMake was not found. Install CMake or add it to PATH."
}

& $CMake -S $NativeDir -B $BuildDir
& $CMake --build $BuildDir --config $Configuration
