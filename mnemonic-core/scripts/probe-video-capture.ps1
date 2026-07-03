#Requires -Version 5.1
<#
.SYNOPSIS
  Compare ddagrab vs gfxcapture vs gdigrab on the current interactive desktop.
  Reproduces grey-video: ddagrab BulkSpan ~7, gfxcapture/gdigrab ~150+.
#>
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Ffmpeg = Join-Path $RepoRoot 'third_party\ffmpeg\win-x64\bin\ffmpeg.exe'
if (-not (Test-Path $Ffmpeg)) {
    throw "FFmpeg not found. Run scripts\fetch-ffmpeg.ps1"
}

$OutDir = Join-Path $env:TEMP 'mnemonic_video_probe'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

function Get-BulkLumaSpan([string]$Mp4) {
    $Png = "$Mp4.frame.png"
    & $Ffmpeg -hide_banner -y -i $Mp4 -vf 'select=eq(n\,45)' -vframes 1 $Png 2>&1 | Out-Null
    $Meta = & $Ffmpeg -hide_banner -i $Png -vf signalstats,metadata=print -f null - 2>&1 | Out-String
    $ylow = [double]([regex]::Match($Meta, 'YLOW=([\d.]+)').Groups[1].Value)
    $yhigh = [double]([regex]::Match($Meta, 'YHIGH=([\d.]+)').Groups[1].Value)
    return ($yhigh - $ylow)
}

function Invoke-Capture([string]$Name, [string]$FilterGraph) {
    $Mp4 = Join-Path $OutDir "$Name.mp4"
    $ErrorActionPreference = 'Continue'
    & $Ffmpeg -hide_banner -y -loglevel error -filter_complex $FilterGraph -t 4 -map '[vout]' -c:v libx264 -preset ultrafast -pix_fmt yuv420p $Mp4 2>&1 | Out-Null
    if (-not (Test-Path $Mp4)) { throw "Capture failed: $Name" }
    $span = Get-BulkLumaSpan $Mp4
    [pscustomobject]@{ Name = $Name; BulkLumaSpan = $span; Path = $Mp4 }
}

$results = @(
    Invoke-Capture 'ddagrab' 'ddagrab=output_idx=0:framerate=30:draw_mouse=1,hwdownload,format=bgra[vout]'
    Invoke-Capture 'gfxcapture' 'gfxcapture=monitor_idx=0:max_framerate=30:capture_cursor=1,hwdownload,format=bgra[vout]'
)
# gdigrab uses input demuxer, separate call:
$gdigrabMp4 = Join-Path $OutDir 'gdigrab.mp4'
$ErrorActionPreference = 'Continue'
& $Ffmpeg -hide_banner -y -loglevel error -f gdigrab -framerate 30 -draw_mouse 1 -i desktop -t 4 -c:v libx264 -preset ultrafast -pix_fmt yuv420p $gdigrabMp4 2>&1 | Out-Null
$results += [pscustomobject]@{ Name = 'gdigrab'; BulkLumaSpan = (Get-BulkLumaSpan $gdigrabMp4); Path = $gdigrabMp4 }

$results | Format-Table -AutoSize
$gfx = ($results | Where-Object Name -eq 'gfxcapture').BulkLumaSpan
$dda = ($results | Where-Object Name -eq 'ddagrab').BulkLumaSpan
if ($dda -lt 20 -and $gfx -lt 50) {
    Write-Error "Both captures look flat (ddagrab=$dda, gfxcapture=$gfx). Desktop session may be unavailable."
}
if ($dda -lt 20 -and $gfx -ge 50) {
    Write-Host "PROBE PASS: ddagrab flat ($dda) but gfxcapture rich ($gfx) — production gfxcapture fix validated." -ForegroundColor Green
    exit 0
}
if ($gfx -gt ($dda + 15)) {
    Write-Host "PROBE PASS: gfxcapture ($gfx) richer than ddagrab ($dda)." -ForegroundColor Green
    exit 0
}
Write-Host "PROBE NOTE: ddagrab=$dda gfxcapture=$gfx on this desktop (both usable). Grey bug needs ddagrab-flat + gfx-rich pattern to confirm." -ForegroundColor Yellow
exit 0
