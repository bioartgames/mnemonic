using System.Collections.Concurrent;
using System.IO.Pipes;
using NAudio.Wave;

namespace Mnemonic.Capture;

public sealed class WasapiPipePump : IDisposable
{
    private static readonly int SilenceBytesPer10Ms = MnemonicConstants.CaptureAudioSampleRate
        * MnemonicConstants.CaptureAudioChannels
        * 2
        / 100;

    private readonly string _pipeName;
    private readonly Func<IWaveIn> _captureFactory;
    private readonly string _sourceLabel;
    private readonly WasapiPumpLog _log;
    private readonly object _writeGate = new();
    private readonly ConcurrentQueue<byte[]> _pendingChunks = new();
    private readonly byte[] _silenceChunk = new byte[SilenceBytesPer10Ms];

    private IWaveIn? _capture;
    private NamedPipeServerStream? _pipe;
    private CancellationTokenSource? _cts;
    private Thread? _staThread;
    private long _pipeConnectedAtMs;
    private long _lastRealPcmAtMs;
    private bool _loggedFirstPcm;
    private bool _disposed;

    public WasapiPipePump(
        string pipeName,
        Func<IWaveIn> captureFactory,
        string sourceLabel,
        string? logFilePath = null)
    {
        _pipeName = pipeName;
        _captureFactory = captureFactory;
        _sourceLabel = sourceLabel;
        _log = new WasapiPumpLog(logFilePath);
    }

    public void Start(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _staThread = new Thread(RunStaPumpThread)
        {
            IsBackground = true,
            Name = $"WasapiPipePump-{_sourceLabel}",
        };
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _cts?.Cancel();
        }
        catch
        {
            // Best effort.
        }

        if (_staThread is not null)
        {
            try
            {
                _staThread.Join(MnemonicConstants.WasapiPipePumpJoinTimeoutMs);
            }
            catch
            {
                // Best effort.
            }
        }

        _cts?.Dispose();
    }

    private void RunStaPumpThread()
    {
        var token = _cts?.Token ?? CancellationToken.None;

        try
        {
            _capture = _captureFactory();
            _pipe = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    _pipe.WaitForConnectionAsync(token).GetAwaiter().GetResult();
                    break;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            _pipeConnectedAtMs = Environment.TickCount64;
            _log.LogStarted(_sourceLabel, _pipeName);

            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;
            _capture.StartRecording();

            WriteLoop(token);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        finally
        {
            try
            {
                if (_capture is not null)
                {
                    _capture.DataAvailable -= OnDataAvailable;
                    _capture.RecordingStopped -= OnRecordingStopped;
                    _capture.StopRecording();
                    _capture.Dispose();
                    _capture = null;
                }
            }
            catch
            {
                // Best effort.
            }

            try
            {
                _pipe?.Dispose();
                _pipe = null;
            }
            catch
            {
                // Best effort.
            }
        }
    }

    private void WriteLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var wroteAudio = false;

            while (_pendingChunks.TryDequeue(out var chunk))
            {
                WriteBuffer(chunk, chunk.Length);
                wroteAudio = true;
            }

            if (!wroteAudio
                && WasapiPipePumpPadding.ShouldWriteSilence(
                    Environment.TickCount64,
                    _pipeConnectedAtMs,
                    _lastRealPcmAtMs,
                    MnemonicConstants.WasapiPipePumpSilencePadDelayMs))
            {
                WriteBuffer(_silenceChunk, _silenceChunk.Length);
            }

            try
            {
                Thread.Sleep(MnemonicConstants.WasapiPipePumpWriteLoopIntervalMs);
            }
            catch (ThreadInterruptedException)
            {
                break;
            }
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _cts?.Cancel();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded <= 0 || _disposed || _capture is null)
        {
            return;
        }

        try
        {
            var converted = AudioPcmConverter.ConvertToTargetPcm(e.Buffer, e.BytesRecorded, _capture.WaveFormat);
            if (converted.Length > 0)
            {
                _lastRealPcmAtMs = Environment.TickCount64;
                _pendingChunks.Enqueue(converted);
                if (!_loggedFirstPcm)
                {
                    _loggedFirstPcm = true;
                    _log.LogFirstPcm(_sourceLabel, converted.Length, AudioPcmConverter.ComputePeakS16Le(converted));
                }
            }
            else
            {
                _log.LogEmptyConversion(_sourceLabel, e.BytesRecorded, _capture.WaveFormat.ToString());
            }
        }
        catch (Exception ex) when (!IsBenignShutdownException(ex))
        {
            _log.LogPcmConversionError(_sourceLabel, ex);
        }
    }

    private static bool IsBenignShutdownException(Exception ex) =>
        ex is ObjectDisposedException or OperationCanceledException;

    private void WriteBuffer(byte[] buffer, int count)
    {
        if (_pipe is null || !_pipe.IsConnected || count <= 0)
        {
            return;
        }

        try
        {
            lock (_writeGate)
            {
                _pipe.Write(buffer, 0, count);
            }
        }
        catch (IOException)
        {
            _cts?.Cancel();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WasapiPipePump));
        }
    }
}
