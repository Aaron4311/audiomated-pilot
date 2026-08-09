<#
.SYNOPSIS
  Publishes Audio.CLI/Audio.GUI/Audio.Service self-contained and compiles the
  Inno Setup installer. Used both locally and by the GitHub Actions release
  workflow, so the two never drift apart.
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "0.0.0-dev"
)

$ErrorActionPreference = "Stop"

$installerDir = $PSScriptRoot
$root = Split-Path -Parent $installerDir
$publishRoot = Join-Path $installerDir "publish"

if (Test-Path $publishRoot) {
    Remove-Item $publishRoot -Recurse -Force
}

function Publish-Project {
    param([string]$Project, [string]$Subfolder)

    $out = Join-Path $publishRoot $Subfolder
    Write-Host "Publishing $Project -> $out"
    dotnet publish (Join-Path $root "src\$Project\$Project.csproj") `
        -c $Configuration -r $Runtime --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $out
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $Project" }
}

Publish-Project -Project "Audio.CLI" -Subfolder "cli"
Publish-Project -Project "Audio.GUI" -Subfolder "gui"
Publish-Project -Project "Audio.Service" -Subfolder "service"

$iscc = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    $iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $iscc)) {
    throw "Inno Setup compiler (ISCC.exe) not found. Install from https://jrsoftware.org/isinfo.php or 'winget install JRSoftware.InnoSetup'."
}

Write-Host "Compiling installer with $iscc"
& $iscc "/DMyAppVersion=$Version" (Join-Path $installerDir "AudiomatedPilot.iss")
if ($LASTEXITCODE -ne 0) { throw "ISCC compilation failed" }

Write-Host "Installer built: $(Join-Path $installerDir 'output')"
