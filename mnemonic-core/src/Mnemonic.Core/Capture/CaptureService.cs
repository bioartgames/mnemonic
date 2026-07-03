using System.Diagnostics;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;

namespace Mnemonic.Capture;

public sealed class CaptureService : IDisposable
{
    private readonly DataRootPaths _paths;
    private readonly StatusStore _statusStore;
    private readonly AppSettings _settings;
    private readonly FfmpegResolution _ffmpeg;
    private readonly object _gate = new();

    private CaptureSessionState? _session;
    private Process? _process;
    private bool _ffmpegOk = true;
    private bool _disposed;
    private bool _paused;

    public CaptureService(
        DataRootPaths paths,
        StatusStore statusStore,
        AppSettings settings,
        FfmpegResolution ffmpeg)
    {
        _paths = paths;
        _statusStore = statusStore;
        _settings = settings;
        _ffmpeg = ffmpeg;
        _ffmpegOk = ffmpeg.IsAvailable;
    }

    public event Action<SegmentClosedEventArgs>? SegmentClosed;

    public int CurrentSegmentIndex
    {
        get
        {
            lock (_gate)
            {
                return _session?.CurrentSegmentIndex ?? -1;
            }
        }
    }

    public string? ActiveCapturePrefix
    {
        get
        {
            lock (_gate)
            {
                return _session?.Prefix;
            }
        }
    }

    public bool TryGetCurrentSegmentWindow(out double tOpenUnix, out double tCloseUnix)
    {
        lock (_gate)
        {
            tOpenUnix = 0;
            tCloseUnix = 0;
            if (_session is null || _session.CurrentSegmentIndex < 0)
            {
                return false;
            }

            var segmentDuration = SegmentDurationPolicy.Normalize(_settings.SegmentDurationSeconds);
            var index = _session.CurrentSegmentIndex;
            tOpenUnix = _session.CaptureStartUnix + (index * segmentDuration);
            tCloseUnix = _session.CaptureStartUnix + ((index + 1) * segmentDuration);
            return true;
        }
    }

    public bool IsRecording
    {
        get
        {
            lock (_gate)
            {
                return _process is { HasExited: false };
            }
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            ThrowIfDisposed();

            if (!_ffmpeg.IsAvailable || _ffmpeg.ExecutablePath is null)
            {
                return;
            }

            if (_process is { HasExited: false })
            {
                return;
            }

            var prefix = NewCaptureSegmentPrefix();
            var captureStartUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _session = new CaptureSessionState
            {
                Prefix = prefix,
                CaptureStartUnix = captureStartUnix,
                LastScratchMaxIndex = -1,
                CurrentSegmentIndex = 0,
            };

            var outfilePattern = SegmentTracker.GetScratchSegmentPattern(_paths.ScratchDir, prefix);
            var audioConfig = CaptureAudioConfig.FromSettings(_settings);

            try
            {
                _session.AudioPumpCts = new CancellationTokenSource();
                _session.AudioPumps = CreateAudioPumps(_paths.LogsDir, prefix, audioConfig);

                var pumpToken = _session.AudioPumpCts.Token;
                foreach (var pump in _session.AudioPumps)
                {
                    pump.Start(pumpToken);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffmpeg.ExecutablePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = false,
                };

                var segmentSeconds = SegmentDurationPolicy.Normalize(_settings.SegmentDurationSeconds);
                foreach (var arg in CaptureArgvBuilder.Build(
                    audioConfig,
                    _settings.DrawMouse,
                    segmentSeconds,
                    outfilePattern,
                    prefix))
                {
                    startInfo.ArgumentList.Add(arg);
                }

                _process = Process.Start(startInfo);
                if (_process is null)
                {
                    throw new InvalidOperationException("Process.Start returned null.");
                }

                var logPath = Path.Combine(_paths.LogsDir, $"ffmpeg_{prefix}.log");
                _session.FfmpegLogPath = logPath;
                _session.StderrDrain = new FfmpegStderrDrain();
                _session.StderrDrain.Start(_process, logPath);
            }
            catch (Exception ex)
            {
                DisposeAudioPumps(_session);
                DisposeSessionDrain(_session);
                _session = null;
                _process = null;
                FfmpegProcessCleanup.KillBundledOrphans(_ffmpeg.ExecutablePath);
                WriteStatus(recording: false, state: CaptureStates.Error, segmentIndex: 0, error: $"Failed to start capture: {ex.Message}");
                return;
            }

            _paused = false;
            WriteStatus(recording: true, state: CaptureStates.Recording, segmentIndex: 0, error: "");
        }
    }

    public void PollSegments()
    {
        lock (_gate)
        {
            if (_disposed || _session is null || _process is null || _process.HasExited)
            {
                return;
            }

            var curr = SegmentTracker.ScanMaxSegmentIndex(_paths.ScratchDir, _session.Prefix);
            if (curr < _session.LastScratchMaxIndex)
            {
                _session.LastScratchMaxIndex = curr;
                _session.CurrentSegmentIndex = Math.Max(curr, 0);
                WriteStatus(recording: true, state: CaptureStates.Recording, segmentIndex: _session.CurrentSegmentIndex, error: "");
                return;
            }

            if (curr > _session.LastScratchMaxIndex)
            {
                foreach (var closedIndex in SegmentTracker.GetNewlyClosedIndices(_session.LastScratchMaxIndex, curr))
                {
                    EmitSegmentClosed(closedIndex);
                }

                _session.LastScratchMaxIndex = curr;
                _session.CurrentSegmentIndex = curr;
                WriteStatus(recording: true, state: CaptureStates.Recording, segmentIndex: curr, error: "");
            }
        }
    }

    public void PollProcess()
    {
        lock (_gate)
        {
            if (_disposed || _process is null)
            {
                return;
            }

            if (!_process.HasExited)
            {
                return;
            }

            var exitCode = _process.ExitCode;
            var logPath = _session?.FfmpegLogPath;
            DisposeAudioPumps(_session);
            DisposeSessionDrain(_session);
            FinalizeOpenSegmentIfNeeded();
            _process = null;
            _session = null;
            _paused = false;
            WriteStatus(
                recording: false,
                state: CaptureStates.Error,
                segmentIndex: 0,
                error: FfmpegCaptureExitSummary.BuildUnexpectedExitMessage(exitCode, logPath));
        }
    }

    public void Pause()
    {
        lock (_gate)
        {
            ThrowIfDisposed();

            if (_process is { HasExited: false })
            {
                PauseCore();
            }
            else if (!_paused)
            {
                _paused = true;
                WriteStatus(recording: false, state: CaptureStates.Paused, segmentIndex: 0, error: "");
            }
        }

        FfmpegProcessCleanup.KillBundledOrphans(_ffmpeg.ExecutablePath);
    }

    public void Resume()
    {
        lock (_gate)
        {
            ThrowIfDisposed();

            if (_process is { HasExited: false })
            {
                return;
            }
        }

        Start();
    }

    public void Stop()
    {
        lock (_gate)
        {
            StopCore();
        }

        FfmpegProcessCleanup.KillBundledOrphans(_ffmpeg.ExecutablePath);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopCore();
        }

        FfmpegProcessCleanup.KillBundledOrphans(_ffmpeg.ExecutablePath);
    }

    private void StopCore()
    {
        DisposeAudioPumps(_session);

        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch
        {
            // Best effort; orphan cleanup runs after StopCore.
        }

        DisposeSessionDrain(_session);
        FinalizeOpenSegmentIfNeeded();
        _process = null;
        _session = null;
        _paused = false;
        WriteStatus(recording: false, state: CaptureStates.Idle, segmentIndex: 0, error: "");
    }

    private void PauseCore()
    {
        DisposeAudioPumps(_session);

        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch
        {
            // Best effort; orphan cleanup runs after Pause().
        }

        DisposeSessionDrain(_session);
        TryDeletePartialScratch();
        _process = null;
        _session = null;
        _paused = true;
        WriteStatus(recording: false, state: CaptureStates.Paused, segmentIndex: 0, error: "");
    }

    private void TryDeletePartialScratch()
    {
        if (_session is null)
        {
            return;
        }

        var path = SegmentTracker.GetScratchSegmentPath(
            _paths.ScratchDir,
            _session.Prefix,
            _session.CurrentSegmentIndex);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best effort.
        }
    }

    private static List<WasapiPipePump> CreateAudioPumps(
        string logsDir,
        string prefix,
        CaptureAudioConfig audioConfig)
    {
        var pumps = new List<WasapiPipePump>();

        if (audioConfig.HasMic)
        {
            var micLog = Path.Combine(logsDir, $"audio_{prefix}_mic.log");
            pumps.Add(new WasapiPipePump(
                CapturePipeNames.GetMicPipeName(prefix),
                () => WasapiCaptureFactory.CreateMicCapture(audioConfig.MicDeviceId),
                "mic",
                micLog));
        }

        if (audioConfig.HasDesktop)
        {
            var desktopLog = Path.Combine(logsDir, $"audio_{prefix}_desktop.log");
            pumps.Add(new WasapiPipePump(
                CapturePipeNames.GetDesktopPipeName(prefix),
                () => WasapiCaptureFactory.CreateLoopbackCapture(audioConfig.DesktopLoopbackDeviceId),
                "desktop",
                desktopLog));
        }

        return pumps;
    }

    private static void DisposeAudioPumps(CaptureSessionState? session)
    {
        if (session is null)
        {
            return;
        }

        try
        {
            session.AudioPumpCts?.Cancel();
        }
        catch
        {
            // Best effort.
        }

        if (session.AudioPumps is not null)
        {
            foreach (var pump in session.AudioPumps)
            {
                pump.Dispose();
            }

            session.AudioPumps.Clear();
        }

        session.AudioPumpCts?.Dispose();
        session.AudioPumpCts = null;
    }

    private void FinalizeOpenSegmentIfNeeded()
    {
        if (_session is null)
        {
            return;
        }

        if (_session.CurrentSegmentIndex < 0)
        {
            return;
        }

        EmitSegmentClosed(_session.CurrentSegmentIndex);
    }

    private void EmitSegmentClosed(int index)
    {
        if (_session is null)
        {
            return;
        }

        if (!_session.EmittedCloseIndices.Add(index))
        {
            return;
        }

        var segmentDuration = SegmentDurationPolicy.Normalize(_settings.SegmentDurationSeconds);
        var args = new SegmentClosedEventArgs
        {
            CapturePrefix = _session.Prefix,
            Index = index,
            ScratchPath = SegmentTracker.GetScratchSegmentPath(_paths.ScratchDir, _session.Prefix, index),
            TOpenUnix = _session.CaptureStartUnix + (index * segmentDuration),
            TCloseUnix = _session.CaptureStartUnix + ((index + 1) * segmentDuration),
        };

        SegmentClosed?.Invoke(args);
    }

    private void WriteStatus(bool recording, string state, int segmentIndex, string error)
    {
        string capturePrefix;
        lock (_gate)
        {
            capturePrefix = recording && _session is not null ? _session.Prefix : "";
        }

        _statusStore.Write(new StatusSnapshot
        {
            ContractVersion = MnemonicConstants.IpcContractVersion,
            Recording = recording,
            State = state,
            FfmpegOk = _ffmpegOk,
            CurrentSegmentIndex = segmentIndex,
            CapturePrefix = capturePrefix,
            DataRoot = _paths.Root,
            Error = error,
        });
    }

    private static string NewCaptureSegmentPrefix()
    {
        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hex = Random.Shared.Next(0, 0xFFFF);
        return $"mn_{unix}_{hex:x4}";
    }

    private void DisposeSessionDrain(CaptureSessionState? session)
    {
        session?.StderrDrain?.Dispose();
        if (session is not null)
        {
            session.StderrDrain = null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CaptureService));
        }
    }
}
