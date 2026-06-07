param(
    [string]$Source = "UnityProject/Builds/WebGL",
    [string]$Destination = "docs"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$SourcePath = Join-Path $Root $Source
$DestinationPath = Join-Path $Root $Destination

if (-not (Test-Path -LiteralPath $SourcePath)) {
    throw "WebGL build output was not found: $SourcePath"
}

$ResolvedRoot = (Resolve-Path -LiteralPath $Root).Path
if (Test-Path -LiteralPath $DestinationPath) {
    $ResolvedDestination = (Resolve-Path -LiteralPath $DestinationPath).Path
    if (-not $ResolvedDestination.StartsWith($ResolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove destination outside workspace: $ResolvedDestination"
    }

    Remove-Item -LiteralPath $ResolvedDestination -Recurse -Force
}

New-Item -ItemType Directory -Path $DestinationPath | Out-Null
Copy-Item -Path (Join-Path $SourcePath "*") -Destination $DestinationPath -Recurse -Force
New-Item -ItemType File -Path (Join-Path $DestinationPath ".nojekyll") -Force | Out-Null

Write-Output "GitHub Pages files prepared at: $DestinationPath"
