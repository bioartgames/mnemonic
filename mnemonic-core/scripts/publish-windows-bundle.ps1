#Requires -Version 5.1
<#
.SYNOPSIS
  Publish self-contained Mnemonic.Windows bundle for Mnemonic release zip.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDir,
    [ValidateSet('Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Csproj = Join-Path $RepoRoot 'src\Mnemonic.Windows\Mnemonic.Windows.csproj'
$FfmpegExe = Join-Path $RepoRoot 'third_party\ffmpeg\win-x64\bin\ffmpeg.exe'
$LicenseSrc = Join-Path $RepoRoot 'third_party\ffmpeg\LICENSE-FFMPEG.txt'

if (-not (Test-Path -LiteralPath $FfmpegExe) -or -not (Test-Path -LiteralPath $LicenseSrc)) {
    Write-Host 'Fetching bundled FFmpeg...'
    & powershell -File (Join-Path $RepoRoot 'scripts\fetch-ffmpeg.ps1')
}

if (-not (Test-Path -LiteralPath $FfmpegExe)) {
    throw "Bundled FFmpeg missing after fetch: $FfmpegExe"
}

if (Test-Path -LiteralPath $OutputDir) {
    Remove-Item -LiteralPath $OutputDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "Publishing self-contained bundle to $OutputDir ..."
dotnet publish $Csproj -c $Configuration -r win-x64 --self-contained true -o $OutputDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$HostExe = Join-Path $OutputDir 'Mnemonic.Windows.exe'
$BundledFfmpeg = Join-Path $OutputDir 'ffmpeg\bin\ffmpeg.exe'
if (-not (Test-Path -LiteralPath $HostExe)) {
    throw "Host executable not found: $HostExe"
}
if (-not (Test-Path -LiteralPath $BundledFfmpeg)) {
    throw "Bundled ffmpeg not found: $BundledFfmpeg"
}

if (-not (Test-Path -LiteralPath $LicenseSrc)) {
    throw "FFmpeg license file missing: $LicenseSrc"
}
Copy-Item -LiteralPath $LicenseSrc -Destination (Join-Path $OutputDir 'LICENSE-FFMPEG.txt') -Force

Write-Host "Published bundle: $OutputDir"
