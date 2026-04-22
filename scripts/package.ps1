param(
    [string]$Version = "1.0.0",
    [string]$Runtime = "win-x64",
    [switch]$CreateInstaller
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $scriptDir "..")

$configuration = "Release"
$framework = "net9.0-windows"

$publishDir = Join-Path $root "artifacts\publish\VSL.UI\$Runtime"
$distDir = Join-Path $root "dist\VSL-$Version-$Runtime"
$zipPath = Join-Path $root "dist\VSL-$Version-$Runtime.zip"
$installerScriptPath = Join-Path $scriptDir "installer.iss"
$installerPath = Join-Path $root "dist\VSL-Setup-$Version-$Runtime.exe"
$totalSteps = if ($CreateInstaller) { 5 } else { 4 }

function Resolve-IsccPath {
    $explicitCandidates = @()

    if (-not [string]::IsNullOrWhiteSpace(${env:ProgramFiles(x86)})) {
        $explicitCandidates += (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe")
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $explicitCandidates += (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    }

    foreach ($candidate in $explicitCandidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    return $null
}

Write-Host "[1/$totalSteps] Cleaning output directories..."
if (Test-Path $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }
if (Test-Path $distDir) { Remove-Item -LiteralPath $distDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
if (Test-Path $installerPath) { Remove-Item -LiteralPath $installerPath -Force }

Write-Host "[2/$totalSteps] Publishing VSL.UI..."
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

Write-Host "[3/$totalSteps] Building portable directory..."
New-Item -ItemType Directory -Path $distDir -Force | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $distDir -Recurse -Force
Copy-Item -Path (Join-Path $root "README.md") -Destination (Join-Path $distDir "README.md") -Force

Write-Host "[4/$totalSteps] Creating zip package..."
Compress-Archive -Path (Join-Path $distDir "*") -DestinationPath $zipPath -Force

if ($CreateInstaller) {
    if (-not (Test-Path -LiteralPath $installerScriptPath)) {
        throw "Installer script not found: $installerScriptPath"
    }

    $isccPath = Resolve-IsccPath
    if ([string]::IsNullOrWhiteSpace($isccPath)) {
        throw "Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6 from https://jrsoftware.org/isinfo.php and re-run with -CreateInstaller."
    }

    Write-Host "[5/$totalSteps] Creating installer package..."
    & $isccPath `
        "/DMyAppVersion=$Version" `
        "/DSourceDir=$distDir" `
        "/DOutputBaseFilename=VSL-Setup-$Version-$Runtime" `
        "/DMyAppExeName=VSL.UI.exe" `
        "/O$(Join-Path $root 'dist')" `
        $installerScriptPath

    if ($LASTEXITCODE -ne 0) {
        throw "ISCC failed with exit code $LASTEXITCODE."
    }
}

$folderSizeMB = [math]::Round(((Get-ChildItem -LiteralPath $distDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB), 2)
$zipSizeMB = [math]::Round(((Get-Item -LiteralPath $zipPath).Length / 1MB), 2)

Write-Host "Done"
Write-Host "Portable folder: $distDir ($folderSizeMB MB)"
Write-Host "Zip package:     $zipPath ($zipSizeMB MB)"
if ($CreateInstaller -and (Test-Path -LiteralPath $installerPath)) {
    $installerSizeMB = [math]::Round(((Get-Item -LiteralPath $installerPath).Length / 1MB), 2)
    Write-Host "Installer:       $installerPath ($installerSizeMB MB)"
}
