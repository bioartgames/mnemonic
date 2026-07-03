namespace Mnemonic.Capture;

public static class ClipIdentity
{
    public static string FormatClipId(string capturePrefix, int segmentIndex)
    {
        if (string.IsNullOrWhiteSpace(capturePrefix))
        {
            throw new ArgumentException("Capture prefix is required.", nameof(capturePrefix));
        }

        if (segmentIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentIndex), "Segment index must be non-negative.");
        }

        return $"{capturePrefix}_segment_{segmentIndex:D5}";
    }

    public static bool TryParseClipId(string clipId, out string capturePrefix, out int segmentIndex)
    {
        capturePrefix = "";
        segmentIndex = -1;
        if (string.IsNullOrWhiteSpace(clipId))
        {
            return false;
        }

        var fileName = clipId;
        if (fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return SegmentTracker.TryParseScratchFileName(fileName, out capturePrefix, out segmentIndex);
        }

        const string marker = "_segment_";
        var pos = fileName.LastIndexOf(marker, StringComparison.Ordinal);
        if (pos <= 0)
        {
            return TryParseLegacySegmentId(fileName, out capturePrefix, out segmentIndex);
        }

        var digits = fileName.Substring(pos + marker.Length);
        if (!int.TryParse(digits, out segmentIndex) || segmentIndex < 0)
        {
            return false;
        }

        capturePrefix = fileName.Substring(0, pos);
        return capturePrefix.Length > 0;
    }

    private static bool TryParseLegacySegmentId(string clipId, out string capturePrefix, out int segmentIndex)
    {
        capturePrefix = "";
        segmentIndex = -1;
        const string legacyPrefix = "segment_";
        if (!clipId.StartsWith(legacyPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var digits = clipId.Substring(legacyPrefix.Length);
        if (!int.TryParse(digits, out segmentIndex) || segmentIndex < 0)
        {
            return false;
        }

        return true;
    }
}
