namespace Mnemonic.Capture;

public static class SegmentTracker
{
    public static int ScanMaxSegmentIndex(string scratchDir, string prefix)
    {
        if (!Directory.Exists(scratchDir))
        {
            return -1;
        }

        var needle = $"{prefix}_segment_";
        var best = -1;

        foreach (var fileName in Directory.EnumerateFiles(scratchDir, "*.mp4"))
        {
            var fname = Path.GetFileName(fileName);
            if (!fname.StartsWith(needle, StringComparison.Ordinal))
            {
                continue;
            }

            if (!fname.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var digits = fname.Substring(needle.Length, fname.Length - needle.Length - 4);
            if (int.TryParse(digits, out var index) && index > best)
            {
                best = index;
            }
        }

        return best;
    }

    public static IReadOnlyList<int> GetNewlyClosedIndices(int previousMax, int currentMax)
    {
        if (currentMax <= previousMax)
        {
            return Array.Empty<int>();
        }

        var start = Math.Max(previousMax, 0);
        var result = new List<int>();
        for (var index = start; index < currentMax; index++)
        {
            result.Add(index);
        }

        return result;
    }

    public static string GetScratchSegmentPath(string scratchDir, string prefix, int index)
    {
        return Path.Combine(scratchDir, $"{prefix}_segment_{index:D5}.mp4");
    }

    public static string GetScratchSegmentPattern(string scratchDir, string prefix)
    {
        return Path.Combine(scratchDir, $"{prefix}_segment_%05d.mp4");
    }

    public static bool TryParseScratchFileName(string fileName, out string prefix, out int index)
    {
        prefix = "";
        index = -1;
        if (!TryParseScratchSegmentIndex(fileName, out index))
        {
            return false;
        }

        const string marker = "_segment_";
        var pos = fileName.LastIndexOf(marker, StringComparison.Ordinal);
        if (pos <= 0)
        {
            index = -1;
            return false;
        }

        prefix = fileName.Substring(0, pos);
        return prefix.Length > 0;
    }

    public static bool TryParseScratchSegmentIndex(string fileName, out int index)
    {
        index = -1;
        if (!fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        const string marker = "_segment_";
        var pos = fileName.LastIndexOf(marker, StringComparison.Ordinal);
        if (pos < 0)
        {
            return false;
        }

        var digits = fileName.Substring(pos + marker.Length, fileName.Length - pos - marker.Length - 4);
        return int.TryParse(digits, out index) && index >= 0;
    }
}
