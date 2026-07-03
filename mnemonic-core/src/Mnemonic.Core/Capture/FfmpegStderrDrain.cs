using System.Diagnostics;
using System.Text;

namespace Mnemonic.Capture;

internal sealed class FfmpegStderrDrain : IDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _copyTask;
    private StreamWriter? _writer;
    private bool _disposed;

    public void Start(Process process, string logFilePath)
    {
        ThrowIfDisposed();

        var directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _cts = new CancellationTokenSource();
        var fileStream = new FileStream(
            logFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        _writer = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = true };

        var writer = _writer;
        var token = _cts.Token;
        _copyTask = Task.Run(async () =>
        {
            try
            {
                await process.StandardError.BaseStream
                    .CopyToAsync(writer.BaseStream, token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on dispose.
            }
            catch (IOException)
            {
                // Process pipe closed.
            }
        }, token);
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

        if (_copyTask is not null)
        {
            try
            {
                _copyTask.Wait(MnemonicConstants.FfmpegStderrDrainJoinTimeoutMs);
            }
            catch
            {
                // Best effort.
            }
        }

        _writer?.Dispose();
        _cts?.Dispose();
        _copyTask = null;
        _writer = null;
        _cts = null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FfmpegStderrDrain));
        }
    }
}
