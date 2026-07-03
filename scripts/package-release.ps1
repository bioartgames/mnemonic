#Requires -Version 5.1
<#
.SYNOPSIS
  Build bundled Core, stage addons/mnemonic/core/, and zip the release addon folder.
#>
param(
    [string]$Version = '',
    [ValidateSet('Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipTest,
    [string]$OutputZip = ''
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$CoreRoot = Join-Path $RepoRoot 'mnemonic-core'
$AddonRoot = Join-Path $RepoRoot 'addons/mnemonic'
$PluginCfg = Join-Path $AddonRoot 'plugin.cfg'
$CoreDest = Join-Path $AddonRoot 'core'
$PublishStaging = Join-Path $RepoRoot 'dist/core-publish'
$Solution = Join-Path $CoreRoot 'Mnemonic.sln'
$PublishScript = Join-Path $CoreRoot 'scripts/publish-windows-bundle.ps1'

if (-not (Test-Path -LiteralPath $Solution)) {
    throw "mnemonic-core solution not found: $Solution"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    if (-not (Test-Path -LiteralPath $PluginCfg)) {
        throw "plugin.cfg not found: $PluginCfg"
    }
    $match = Select-String -Path $PluginCfg -Pattern 'version="([^"]+)"'
    if (-not $match) {
        throw "Could not parse version from $PluginCfg"
    }
    $Version = $match.Matches[0].Groups[1].Value
}

if ([string]::IsNullOrWhiteSpace($OutputZip)) {
    $OutputZip = Join-Path $RepoRoot "dist/mnemonic-$Version.zip"
}

if (-not $SkipTest) {
    Write-Host "Running dotnet test..."
    dotnet test $Solution -c $Configuration --no-restore:$false
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}

Write-Host "Publishing Core bundle..."
& powershell -File $PublishScript -OutputDir $PublishStaging -Configuration $Configuration

$HostExe = Join-Path $PublishStaging 'Mnemonic.Windows.exe'
$BundledFfmpeg = Join-Path $PublishStaging 'ffmpeg/bin/ffmpeg.exe'
$LicenseFile = Join-Path $PublishStaging 'LICENSE-FFMPEG.txt'
foreach ($required in @($HostExe, $BundledFfmpeg, $LicenseFile)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Publish output missing required file: $required"
    }
}

if (Test-Path -LiteralPath $CoreDest) {
    Remove-Item -LiteralPath $CoreDest -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $CoreDest | Out-Null
Copy-Item -Path (Join-Path $PublishStaging '*') -Destination $CoreDest -Recurse -Force
$GdIgnore = Join-Path $CoreDest '.gdignore'
if (-not (Test-Path -LiteralPath $GdIgnore)) {
    New-Item -ItemType File -Force -Path $GdIgnore | Out-Null
}

$ZipStageRoot = Join-Path $RepoRoot 'dist/zip-stage'
$ZipStaging = Join-Path $ZipStageRoot 'mnemonic'
if (Test-Path -LiteralPath $ZipStageRoot) {
    Remove-Item -LiteralPath $ZipStageRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $ZipStaging | Out-Null
Copy-Item -Path (Join-Path $AddonRoot '*') -Destination $ZipStaging -Recurse -Force

$OutputZipParent = Split-Path $OutputZip -Parent
if (-not (Test-Path -LiteralPath $OutputZipParent)) {
    New-Item -ItemType Directory -Force -Path $OutputZipParent | Out-Null
}
if (Test-Path -LiteralPath $OutputZip) {
    Remove-Item -LiteralPath $OutputZip -Force
}
Compress-Archive -Path $ZipStaging -DestinationPath $OutputZip -Force

$zipSize = (Get-Item -LiteralPath $OutputZip).Length
Write-Host ''
Write-Host 'Release package complete:'
Write-Host "  Core exe: $(Join-Path $CoreDest 'Mnemonic.Windows.exe')"
Write-Host "  Zip:      $OutputZip ($zipSize bytes)"
