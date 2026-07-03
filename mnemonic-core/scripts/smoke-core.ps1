#Requires -Version 5.1
<#
.SYNOPSIS
  Core-only Windows E2E smoke test (CRG-104).
.PARAMETER CleanDataRoot
  Delete %LOCALAPPDATA%\Mnemonic before run (fresh DataRoot).
.PARAMETER SkipPublish
  Use existing Release build in bin\ instead of dist\smoke publish.
.PARAMETER FullSegment
  Optional 125s wait to assert current_segment_index >= 1.
#>
param(
    [switch]$CleanDataRoot,
    [switch]$SkipPublish,
    [switch]$FullSegment
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$DataRoot = Join-Path $env:LOCALAPPDATA 'Mnemonic'
$StatusFile = Join-Path $DataRoot 'control\status.json'
$FlagFile = Join-Path $DataRoot 'control\commands\flag_current.json'
$MinScratchBytes = 100000
$RecordingTimeoutSec = 45
$ScratchTimeoutSec = 90
$FlagConsumeTimeoutSec = 5
$PostStopSettleSec = 3
$CrashSettleSec = 6
$ErrorTimeoutSec = 15
$AudioMeanVolumeFloorDb = -55.0
$AudioMaxVolumeFloorDb = -90.0
$VideoBulkLumaSpanFloor = 30.0

function Invoke-ExternalCommand {
    param(
        [string]$Exe,
        [string[]]$Arguments
    )

    $previousEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $Exe @Arguments 2>&1 | Out-String
        return @{
            ExitCode = $LASTEXITCODE
            Output = $output
        }
    }
    finally {
        $ErrorActionPreference = $previousEap
    }
}

function Write-Step([string]$Message) {
    Write-Host "==> $Message"
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        Write-Error "ASSERT FAILED: $Message"
        exit 1
    }
}

function Get-StatusSnapshot {
    if (-not (Test-Path $StatusFile)) {
        return $null
    }
    return Get-Content $StatusFile -Raw | ConvertFrom-Json
}

function Wait-Status {
    param(
        [scriptblock]$Predicate,
        [int]$TimeoutSec,
        [string]$Description
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $s = Get-StatusSnapshot
        if ($null -ne $s -and (& $Predicate $s)) {
            return $s
        }
        Start-Sleep -Milliseconds 500
    }
    throw "Timeout waiting for status: $Description (${TimeoutSec}s)"
}

function Stop-MnemonicHost {
    Stop-Process -Name 'Mnemonic.Windows', 'ffmpeg' -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

function Wait-PreservedClip {
    param([int]$TimeoutSec)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $clips = @(Get-ChildItem (Join-Path $DataRoot 'clips') -Recurse -Filter 'video.mp4' -ErrorAction SilentlyContinue)
        $valid = $clips | Where-Object { $_.Length -ge $MinScratchBytes } | Select-Object -First 1
        if ($null -ne $valid) {
            return $valid
        }
        Start-Sleep -Milliseconds 500
    }
    throw "Timeout waiting for preserved clips/**/video.mp4 >= $MinScratchBytes bytes (${TimeoutSec}s)"
}

function Wait-ScratchMp4 {
    param([int]$TimeoutSec)
    # File may show 0 B briefly while the muxer opens; smoke waits for minimum capture size.
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastBytes = -1
    while ((Get-Date) -lt $deadline) {
        $scratchDir = Join-Path $DataRoot 'scratch'
        if (Test-Path $scratchDir) {
            $files = @(Get-ChildItem $scratchDir -Filter 'mn_*_segment_00000.mp4' -ErrorAction SilentlyContinue)
            if ($files.Count -gt 0) {
                $file = $files[0]
                $lastBytes = $file.Length
                $ffmpeg = Get-Process -Name 'ffmpeg' -ErrorAction SilentlyContinue
                if ($null -ne $ffmpeg -and $lastBytes -ge $MinScratchBytes) {
                    return $file
                }
            }
        }
        Start-Sleep -Milliseconds 500
    }
    $detail = if ($lastBytes -ge 0) { "last size ${lastBytes} bytes" } else { 'no scratch file' }
    throw "Timeout waiting for scratch MP4 >= $MinScratchBytes bytes with ffmpeg running (${TimeoutSec}s; $detail)"
}

function Assert-DataRootLayout {
    $expectedDirs = @(
        (Join-Path $DataRoot 'scratch'),
        (Join-Path $DataRoot 'clips'),
        (Join-Path $DataRoot 'control'),
        (Join-Path $DataRoot 'control\commands'),
        (Join-Path $DataRoot 'events'),
        (Join-Path $DataRoot 'logs')
    )
    foreach ($dir in $expectedDirs) {
        Assert-True (Test-Path $dir) "Missing directory: $dir"
    }
    Assert-True (Test-Path (Join-Path $DataRoot 'settings.json')) 'Missing settings.json'
    Assert-True (Test-Path $StatusFile) 'Missing status.json'
}

function Assert-RecordingStatus($s) {
    Assert-True $s.recording 'Expected recording=true'
    Assert-True ($s.state -eq 'recording') "Expected state=recording, got $($s.state)"
    Assert-True $s.ffmpeg_ok 'Expected ffmpeg_ok=true'
    Assert-True ([string]::IsNullOrEmpty($s.error)) "Expected empty error, got $($s.error)"
    Assert-True ($s.contract_version -eq 1) 'Expected contract_version=1'
    $expectedRoot = (Resolve-Path $DataRoot).Path
    $actualRoot = (Resolve-Path $s.data_root).Path
    Assert-True ($actualRoot -eq $expectedRoot) "data_root mismatch: $actualRoot vs $expectedRoot"
}

function Start-MnemonicHost([string]$HostExe) {
    $proc = Start-Process -FilePath $HostExe -WindowStyle Hidden -PassThru
    return $proc
}

function Get-FfprobeExe([string]$FfmpegExe) {
    $candidate = Join-Path (Split-Path $FfmpegExe -Parent) 'ffprobe.exe'
    Assert-True (Test-Path $candidate) "ffprobe not found beside ffmpeg: $candidate"
    return $candidate
}

function Get-AudioStreamIndexes([string]$FfprobeExe, [string]$VideoPath) {
    $probeArgs = @(
        '-hide_banner', '-loglevel', 'error',
        '-select_streams', 'a',
        '-show_entries', 'stream=index',
        '-of', 'csv=p=0',
        $VideoPath
    )
    $result = Invoke-ExternalCommand -Exe $FfprobeExe -Arguments $probeArgs
    if ($result.ExitCode -ne 0) {
        throw "ffprobe stream discovery failed (exit $($result.ExitCode)): $($result.Output)"
    }

    return @($result.Output -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })
}

function Get-VolumedetectStats([string]$FfmpegExe, [string]$VideoPath, [int]$StreamIndex) {
    $detectArgs = @(
        '-hide_banner', '-nostats',
        '-i', $VideoPath,
        '-map', "0:a:$StreamIndex",
        '-af', 'volumedetect',
        '-f', 'null', '-'
    )
    $result = Invoke-ExternalCommand -Exe $FfmpegExe -Arguments $detectArgs
    if ($result.ExitCode -ne 0) {
        throw "volumedetect failed for stream ${StreamIndex} (exit $($result.ExitCode)): $($result.Output)"
    }

    $meanMatch = [regex]::Match($result.Output, 'mean_volume:\s*(-?\d+(?:\.\d+)?)\s*dB')
    $maxMatch = [regex]::Match($result.Output, 'max_volume:\s*(-?\d+(?:\.\d+)?)\s*dB')
    if (-not $meanMatch.Success -or -not $maxMatch.Success) {
        throw "volumedetect output missing mean/max volume for stream ${StreamIndex}: $($result.Output)"
    }

    return @{
        MeanDb = [double]$meanMatch.Groups[1].Value
        MaxDb = [double]$maxMatch.Groups[1].Value
    }
}

function Assert-NoAudioPumpConversionErrors([string]$LogsDir) {
    $audioLogs = @(Get-ChildItem $LogsDir -Filter 'audio_*_*.log' -ErrorAction SilentlyContinue)
    foreach ($log in $audioLogs) {
        $text = Get-Content $log.FullName -Raw -ErrorAction SilentlyContinue
        if ($text -match 'pcm conversion error') {
            throw "Audio pump log contains PCM conversion errors: $($log.FullName)"
        }
    }
}

function Get-VideoBulkLumaSpan([string]$FfmpegExe, [string]$VideoPath) {
    $png = "$VideoPath.vprobe.png"
    $extractArgs = @(
        '-hide_banner', '-y',
        '-i', $VideoPath,
        '-vf', 'select=eq(n\,45)',
        '-vframes', '1',
        $png
    )
    $extract = Invoke-ExternalCommand -Exe $FfmpegExe -Arguments $extractArgs
    if ($extract.ExitCode -ne 0) {
        throw "video frame extract failed: $($extract.Output)"
    }

    $statsArgs = @('-hide_banner', '-i', $png, '-vf', 'signalstats,metadata=print', '-f', 'null', '-')
    $stats = Invoke-ExternalCommand -Exe $FfmpegExe -Arguments $statsArgs
    Remove-Item $png -Force -ErrorAction SilentlyContinue
    if ($stats.ExitCode -ne 0) {
        throw "video signalstats failed: $($stats.Output)"
    }

    $ylow = [double]([regex]::Match($stats.Output, 'YLOW=([\d.]+)').Groups[1].Value)
    $yhigh = [double]([regex]::Match($stats.Output, 'YHIGH=([\d.]+)').Groups[1].Value)
    return ($yhigh - $ylow)
}

function Assert-PreservedClipVideo([string]$FfmpegExe, [string]$VideoPath) {
    $bulkSpan = Get-VideoBulkLumaSpan $FfmpegExe $VideoPath
    Assert-True ($bulkSpan -gt $VideoBulkLumaSpanFloor) (
        "Preserved clip video looks flat/grey (bulk luma span=${bulkSpan}, expected > ${VideoBulkLumaSpanFloor}). Re-run scripts\probe-video-capture.ps1."
    )
    Write-Step "Video bulk luma span=${bulkSpan} (not flat grey)"
}

function Assert-PreservedClipAudio([string]$FfmpegExe, [string]$VideoPath, [string]$LogsDir) {
    Assert-NoAudioPumpConversionErrors $LogsDir

    $ffprobeExe = Get-FfprobeExe $FfmpegExe
    $audioStreams = @(Get-AudioStreamIndexes $ffprobeExe $VideoPath)
    if ($audioStreams.Count -eq 0) {
        throw 'Preserved clip has no audio streams (expected AAC when MNEMONIC_SMOKE seeds WASAPI devices)'
    }

    $streamNum = 0
    $anyAboveSilenceFloor = $false
    $bestMeanDb = [double]::NegativeInfinity
    foreach ($index in $audioStreams) {
        $stats = Get-VolumedetectStats $FfmpegExe $VideoPath $streamNum
        if ($stats.MaxDb -gt $AudioMaxVolumeFloorDb) {
            $anyAboveSilenceFloor = $true
        }

        if ($stats.MeanDb -gt $bestMeanDb) {
            $bestMeanDb = $stats.MeanDb
        }

        Write-Step (
            "Audio stream $streamNum mean_volume=$($stats.MeanDb) dB max_volume=$($stats.MaxDb) dB (ffprobe index $index)"
        )
        $streamNum++
    }

    Assert-True $anyAboveSilenceFloor (
        "All audio streams are padding-level silence (max_volume <= ${AudioMaxVolumeFloorDb} dB). " +
        'Ensure loopback captured the smoke beeps or desktop audio was active.'
    )

    Assert-True ($bestMeanDb -gt $AudioMeanVolumeFloorDb) (
        "Loudest track mean_volume ${bestMeanDb} dB <= floor ${AudioMeanVolumeFloorDb} dB"
    )
}

function Invoke-SmokeLoopbackBeeps {
    try {
        [console]::Beep(880, 350)
        Start-Sleep -Milliseconds 150
        [console]::Beep(1175, 350)
    }
    catch {
        Write-Step 'Console beep unavailable; relying on ambient desktop audio for loopback check'
    }
}

try {
    Set-Location $RepoRoot
    $env:MNEMONIC_SMOKE = '1'

    # Phase 0 — Preflight
    Write-Step 'Phase 0: Preflight'
    Stop-MnemonicHost
    if ($CleanDataRoot -and (Test-Path $DataRoot)) {
        Write-Step "Removing DataRoot: $DataRoot"
        Remove-Item -Path $DataRoot -Recurse -Force
    }

    # Phase 1 — Build publish artifact
    Write-Step 'Phase 1: Build and publish'
    $ffmpegSource = Join-Path $RepoRoot 'third_party\ffmpeg\win-x64\bin\ffmpeg.exe'
    if (-not (Test-Path $ffmpegSource)) {
        Write-Step 'Fetching bundled FFmpeg'
        & powershell -File (Join-Path $RepoRoot 'scripts\fetch-ffmpeg.ps1')
    }

    if ($SkipPublish) {
        $HostExe = Join-Path $RepoRoot 'src\Mnemonic.Windows\bin\Release\net8.0-windows\Mnemonic.Windows.exe'
        if (-not (Test-Path $HostExe)) {
            Write-Step 'Release build missing; building Release'
            dotnet build (Join-Path $RepoRoot 'src\Mnemonic.Windows\Mnemonic.Windows.csproj') -c Release
        }
    }
    else {
        $publishDir = Join-Path $RepoRoot 'dist\smoke'
        if (Test-Path $publishDir) {
            Remove-Item -Path $publishDir -Recurse -Force
        }
        dotnet publish (Join-Path $RepoRoot 'src\Mnemonic.Windows\Mnemonic.Windows.csproj') -c Release -o $publishDir
        $HostExe = Join-Path $publishDir 'Mnemonic.Windows.exe'
    }

    Assert-True (Test-Path $HostExe) "Host executable not found: $HostExe"
    $ffmpegBundled = Join-Path (Split-Path $HostExe -Parent) 'ffmpeg\bin\ffmpeg.exe'
    Assert-True (Test-Path $ffmpegBundled) "Bundled ffmpeg not found: $ffmpegBundled"

    # Phase 2 — Recording + DataRoot
    Write-Step 'Phase 2: Recording and DataRoot layout'
    $hostProc = Start-MnemonicHost $HostExe
    $status = Wait-Status -Predicate { param($s) $s.recording -and $s.state -eq 'recording' } -TimeoutSec $RecordingTimeoutSec -Description 'recording'
    Assert-RecordingStatus $status
    Assert-DataRootLayout
    $scratchFile = Wait-ScratchMp4 -TimeoutSec $ScratchTimeoutSec
    Write-Step "Scratch segment: $($scratchFile.Name) ($($scratchFile.Length) bytes)"
    Invoke-SmokeLoopbackBeeps
    Start-Sleep -Seconds 2

    # Phase 3 — Flag consume + preserve (finalize via ffmpeg exit + process poll)
    Write-Step 'Phase 3: Flag consume and preserve'
    Set-Content -Path $FlagFile -Value '{}' -Encoding utf8
    $flagDeadline = (Get-Date).AddSeconds($FlagConsumeTimeoutSec)
    while ((Get-Date) -lt $flagDeadline) {
        if (-not (Test-Path $FlagFile)) { break }
        Start-Sleep -Milliseconds 200
    }
    Assert-True (-not (Test-Path $FlagFile)) 'flag_current.json was not consumed'

    $ffmpegForPreserve = Get-Process -Name 'ffmpeg' -ErrorAction SilentlyContinue | Select-Object -First 1
    Assert-True ($null -ne $ffmpegForPreserve) 'ffmpeg not running before preserve finalize'
    Stop-Process -Id $ffmpegForPreserve.Id -Force
    Start-Sleep -Seconds $PostStopSettleSec

    $validClip = Wait-PreservedClip -TimeoutSec $ErrorTimeoutSec
    Write-Step "Preserved clip: $($validClip.FullName) ($($validClip.Length) bytes)"

    Write-Step 'Phase 3b: Video and audio checks on preserved clip'
    Assert-PreservedClipVideo $ffmpegBundled $validClip.FullName
    Assert-PreservedClipAudio $ffmpegBundled $validClip.FullName (Join-Path $DataRoot 'logs')

    foreach ($segmentDir in Get-ChildItem (Join-Path $DataRoot 'clips') -Directory -Filter 'segment_*') {
        $clipJsonPath = Join-Path $segmentDir.FullName 'clip.json'
        Assert-True (Test-Path $clipJsonPath) "clip.json should exist in $($segmentDir.Name)"
        $clipMeta = Get-Content $clipJsonPath -Raw | ConvertFrom-Json
        Assert-True ($clipMeta.id -eq $segmentDir.Name) "clip.json id should match folder name"
        Assert-True ($clipMeta.duration_seconds -eq 120) 'clip.json duration_seconds should be 120'
        Assert-True ($null -ne $clipMeta.score) 'clip.json score should be present'
    }

    Stop-MnemonicHost

    # Phase 4 — FFmpeg crash
    Write-Step 'Phase 4: FFmpeg crash (no restart)'
    $hostProc2 = Start-MnemonicHost $HostExe
    Wait-Status -Predicate { param($s) $s.recording } -TimeoutSec $RecordingTimeoutSec -Description 'recording for crash test' | Out-Null
    Wait-ScratchMp4 -TimeoutSec $ScratchTimeoutSec | Out-Null

    $ffmpegProc = Get-Process -Name 'ffmpeg' -ErrorAction SilentlyContinue | Select-Object -First 1
    Assert-True ($null -ne $ffmpegProc) 'ffmpeg process not found before kill'
    Stop-Process -Id $ffmpegProc.Id -Force

    $errStatus = Wait-Status -Predicate { param($s) $s.state -eq 'error' } -TimeoutSec $ErrorTimeoutSec -Description 'error after ffmpeg kill'
    Assert-True (-not $errStatus.recording) 'Expected recording=false after ffmpeg kill'
    Assert-True ($errStatus.error -like 'FFmpeg capture exited (code *') "Unexpected error: $($errStatus.error)"

    Start-Sleep -Seconds $CrashSettleSec
    $ffmpegAfter = Get-Process -Name 'ffmpeg' -ErrorAction SilentlyContinue
    Assert-True ($null -eq $ffmpegAfter) 'ffmpeg was restarted after crash (should not happen)'

    Stop-MnemonicHost
    $hostAfter = Get-Process -Name 'Mnemonic.Windows', 'ffmpeg' -ErrorAction SilentlyContinue
    Assert-True ($null -eq $hostAfter) 'Orphan Mnemonic.Windows or ffmpeg after stop'

    # Phase 5 — Optional full segment
    if ($FullSegment) {
        Write-Step 'Phase 5: Full segment rollover (125s)'
        if (Test-Path $DataRoot) {
            Remove-Item -Path $DataRoot -Recurse -Force
        }
        Start-MnemonicHost $HostExe | Out-Null
        Wait-Status -Predicate { param($s) $s.recording } -TimeoutSec $RecordingTimeoutSec -Description 'recording for full segment' | Out-Null
        Start-Sleep -Seconds 125
        $segStatus = Get-StatusSnapshot
        Assert-True ($segStatus.current_segment_index -ge 1) "Expected current_segment_index >= 1, got $($segStatus.current_segment_index)"
        Stop-MnemonicHost
    }

    Write-Host ''
    Write-Host 'SMOKE PASS' -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ''
    Write-Host "SMOKE FAIL: $($_.Exception.Message)" -ForegroundColor Red
    Stop-MnemonicHost
    exit 1
}
