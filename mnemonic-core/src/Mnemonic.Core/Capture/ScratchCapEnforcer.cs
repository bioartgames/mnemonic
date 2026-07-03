namespace Mnemonic.Capture;

public static class ScratchCapEnforcer
{
    public static ScratchCapResult Enforce(string scratchDir, long capBytes)
    {
        var entries = ListSegments(scratchDir);

        var total = entries.Sum(entry => entry.Size);
        var evicted = 0;
        var safetyIterations = entries.Count + 128;

        while (total > capBytes && entries.Count > 1 && safetyIterations > 0)
        {
            safetyIterations--;

            var oldest = entries[0];
            var ageSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - oldest.MtimeUnix;
            if (ageSeconds < MnemonicConstants.ScratchCapActiveGraceSeconds)
            {
                break;
            }

            try
            {
                File.Delete(oldest.Path);
            }
            catch (IOException)
            {
                break;
            }

            total -= oldest.Size;
            entries.RemoveAt(0);
            evicted++;
        }

        return new ScratchCapResult(total, evicted);
    }

    private static List<ScratchSegmentEntry> ListSegments(string scratchDir)
    {
        if (!Directory.Exists(scratchDir))
        {
            return [];
        }

        var collected = new List<ScratchSegmentEntry>();

        foreach (var path in Directory.EnumerateFiles(scratchDir, "*.mp4"))
        {
            var fileName = Path.GetFileName(path);
            if (!SegmentTracker.TryParseScratchSegmentIndex(fileName, out _))
            {
                continue;
            }

            if (!File.Exists(path))
            {
                continue;
            }

            var info = new FileInfo(path);
            var mtimeUnix = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds();
            collected.Add(new ScratchSegmentEntry(path, info.Length, mtimeUnix));
        }

        collected.Sort((a, b) => a.MtimeUnix.CompareTo(b.MtimeUnix));
        return collected;
    }
}
