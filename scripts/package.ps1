param(
    [string]$Version = "1.0.0",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $scriptDir "..")

$configuration = "Release"
$framework = "net9.0-windows"

$publishDir = Join-Path $root "artifacts\publish\VSL.UI\$Runtime"
$distDir = Join-Path $root "dist\VSL-$Version-$Runtime"
$zipPath = Join-Path $root "dist\VSL-$Version-$Runtime.zip"

Write-Host "[1/4] Cleaning output directories..."
if (Test-Path $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }
if (Test-Path $distDir) { Remove-Item -LiteralPath $distDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

Write-Host "[2/4] Publishing VSL.UI..."
dotnet publish (Join-Path $root "VSL.UI\VSL.UI.csproj") `
    -c $configuration `
    -f $framework `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:DebugSymbols=false `
    -p:DebugType=None `
    -o $publishDir

Write-Host "[3/4] Building portable directory..."
New-Item -ItemType Directory -Path $distDir -Force | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $distDir -Recurse -Force
Copy-Item -Path (Join-Path $root "README.md") -Destination (Join-Path $distDir "README.md") -Force

Write-Host "[4/4] Creating zip package..."
Compress-Archive -Path (Join-Path $distDir "*") -DestinationPath $zipPath -Force

$folderSizeMB = [math]::Round(((Get-ChildItem -LiteralPath $distDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB), 2)
$zipSizeMB = [math]::Round(((Get-Item -LiteralPath $zipPath).Length / 1MB), 2)

Write-Host "Done"
Write-Host "Portable folder: $distDir ($folderSizeMB MB)"
Write-Host "Zip package:     $zipPath ($zipSizeMB MB)"