param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$NativeDir = Join-Path $Root "native"
$BuildDir = Join-Path $NativeDir "build"

cmake -S $NativeDir -B $BuildDir
cmake --build $BuildDir --config $Configuration
