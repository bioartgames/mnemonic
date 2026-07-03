namespace Mnemonic.Capture;

public sealed record ScratchStaleCleanupResult(
    int Scanned = 0,
    int Deleted = 0,
    int SkippedProtected = 0,
    int SkippedRecent = 0,
    int Failed = 0);

public static class ScratchStaleCleanup
{
    public static ScratchStaleCleanupResult Enforce(
        string scratchDir,
        bool recording,
        string? activePrefix,
        int activeSegmentIndex,
        long nowUnix,
        Action<string>? onDeleted = null)
    {
        var result = new ScratchStaleCleanupResult();
        if (!Directory.Exists(scratchDir))
        {
            return result;
        }

        var graceSeconds = recording
            ? MnemonicConstants.ScratchStaleGraceSecondsRecording
            : MnemonicConstants.ScratchStaleGraceSecondsIdle;
        var hasActivePrefix = recording && !string.IsNullOrEmpty(activePrefix);

        foreach (var path in Directory.EnumerateFiles(scratchDir, "*.mp4"))
        {
            if (result.Deleted >= MnemonicConstants.ScratchStaleCleanupMaxDeletesPerRun)
            {
                break;
            }

            var fileName = Path.GetFileName(path);
            if (!SegmentTracker.TryParseScratchSegmentIndex(fileName, out var index))
            {
                continue;
            }

            result = result with { Scanned = result.Scanned + 1 };

            if (hasActivePrefix
                && SegmentTracker.TryParseScratchFileName(fileName, out var prefix, out var parsedIndex)
                && string.Equals(prefix, activePrefix, StringComparison.Ordinal)
                && parsedIndex >= activeSegmentIndex)
            {
                result = result with { SkippedProtected = result.SkippedProtected + 1 };
                continue;
            }

            if (!File.Exists(path))
            {
                continue;
            }

            var mtimeUnix = new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds();
            var ageSeconds = nowUnix - mtimeUnix;
            if (ageSeconds < graceSeconds)
            {
                result = result with { SkippedRecent = result.SkippedRecent + 1 };
                continue;
            }

            try
            {
                File.Delete(path);
                result = result with { Deleted = result.Deleted + 1 };
                onDeleted?.Invoke(path);
            }
            catch (IOException)
            {
                result = result with { Failed = result.Failed + 1 };
            }
        }

        return result;
    }
}
