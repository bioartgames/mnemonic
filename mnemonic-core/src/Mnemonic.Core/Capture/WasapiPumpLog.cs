namespace Mnemonic.Capture;

internal sealed class WasapiPumpLog
{
    private readonly string? _logFilePath;
    private readonly object _gate = new();
    private DateTimeOffset _lastConversionErrorLogged = DateTimeOffset.MinValue;

    public WasapiPumpLog(string? logFilePath)
    {
        _logFilePath = string.IsNullOrWhiteSpace(logFilePath) ? null : logFilePath;
    }

    public void LogPcmConversionError(string sourceLabel, Exception ex)
    {
        if (_logFilePath is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            if ((now - _lastConversionErrorLogged).TotalMilliseconds
                < MnemonicConstants.WasapiPipePumpLogThrottleMs)
            {
                return;
            }

            _lastConversionErrorLogged = now;
            AppendLine($"[{now:O}] {sourceLabel} pcm conversion error: {ex}");
        }
    }

    public void LogStarted(string sourceLabel, string pipeName)
    {
        AppendLine($"[{DateTimeOffset.UtcNow:O}] {sourceLabel} pump started (pipe={pipeName})");
    }

    public void LogFirstPcm(string sourceLabel, int convertedBytes, int peakS16)
    {
        AppendLine(
            $"[{DateTimeOffset.UtcNow:O}] {sourceLabel} first pcm chunk bytes={convertedBytes} peak_s16={peakS16}");
    }

    public void LogEmptyConversion(string sourceLabel, int bytesRecorded, string waveFormat)
    {
        AppendLine(
            $"[{DateTimeOffset.UtcNow:O}] {sourceLabel} conversion produced 0 bytes from {bytesRecorded} raw bytes ({waveFormat})");
    }

    private void AppendLine(string line)
    {
        if (_logFilePath is null)
        {
            return;
        }

        lock (_gate)
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
            catch
            {
                // Best effort; do not break capture.
            }
        }
    }
}
