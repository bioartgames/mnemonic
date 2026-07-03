namespace Mnemonic.Capture;

internal static class FfmpegCaptureExitSummary
{
    public static string BuildUnexpectedExitMessage(int exitCode, string? logFilePath)
    {
        var message = $"FFmpeg capture exited (code {exitCode})";
        if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
        {
            return message;
        }

        var tail = ReadTail(logFilePath, MnemonicConstants.FfmpegCaptureExitStatusTailChars);
        if (tail.Length == 0)
        {
            return message;
        }

        return $"{message}: {tail}";
    }

    private static string ReadTail(string path, int maxChars)
    {
        try
        {
            var text = File.ReadAllText(path);
            return Truncate(text, maxChars);
        }
        catch
        {
            return "";
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[^maxLength..];
    }
}
