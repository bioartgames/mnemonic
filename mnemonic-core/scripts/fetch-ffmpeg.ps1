#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$ReleaseTag = "autobuild-2026-07-08-13-30"
$ZipName = "ffmpeg-N-125505-gc57660fb18-win64-gpl-shared.zip"
$ZipUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/$ReleaseTag/$ZipName"
# gpl-shared ships ffmpeg.exe plus required DLLs in bin/ (win64-gpl bin is exe-only here)
$ExpectedSha256 = "2F93D54008EA10F6DACAD73CBE8EC9041E9B1BD3A85972FAFFB58F8D5E35C90A"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$DestBin = Join-Path $RepoRoot "third_party\ffmpeg\win-x64\bin"
$LicenseDest = Join-Path $RepoRoot "third_party\ffmpeg\LICENSE-FFMPEG.txt"

$TempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("mnemonic-ffmpeg-" + [guid]::NewGuid().ToString("N"))
$ZipPath = Join-Path $TempDir $ZipName

try {
    New-Item -ItemType Directory -Force -Path $TempDir | Out-Null
    New-Item -ItemType Directory -Force -Path $DestBin | Out-Null

    Write-Host "Downloading $ZipUrl ..."
    Invoke-WebRequest -Uri $ZipUrl -OutFile $ZipPath -UseBasicParsing

    $actualHash = (Get-FileHash -Path $ZipPath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualHash -ne $ExpectedSha256) {
        throw "SHA256 mismatch. Expected $ExpectedSha256 got $actualHash"
    }

    Write-Host "Extracting ..."
    Expand-Archive -Path $ZipPath -DestinationPath $TempDir -Force

    $extractRoot = Get-ChildItem -Path $TempDir -Directory | Where-Object { $_.Name -like "ffmpeg-*" } | Select-Object -First 1
    if (-not $extractRoot) {
        throw "Could not find ffmpeg extract folder in $TempDir"
    }

    $sourceBin = Join-Path $extractRoot.FullName "bin"
    if (-not (Test-Path (Join-Path $sourceBin "ffmpeg.exe"))) {
        throw "ffmpeg.exe not found in $($extractRoot.FullName)"
    }

    Write-Host "Copying bin/ to $DestBin ..."
    Get-ChildItem -Path $DestBin -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Copy-Item -Path (Join-Path $sourceBin "*") -Destination $DestBin -Recurse -Force

    $licenseSrc = Join-Path $extractRoot.FullName "LICENSE"
    if (Test-Path $licenseSrc) {
        Copy-Item -Path $licenseSrc -Destination $LicenseDest -Force
    }
    if (-not (Test-Path $LicenseDest)) {
        @"
FFmpeg binaries are provided under the GNU GPL version 2 or later.
See https://ffmpeg.org/legal.html and https://github.com/BtbN/FFmpeg-Builds
Build: $ReleaseTag ($ZipName)
"@ | Set-Content -Path $LicenseDest -Encoding utf8
    }

    $ffmpegExe = Join-Path $DestBin "ffmpeg.exe"
    Write-Host "Done: $ffmpegExe"
}
finally {
    if (Test-Path $TempDir) {
        Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
