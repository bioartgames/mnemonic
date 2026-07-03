namespace Mnemonic.Retention;

public sealed class SegmentHistoryStore
{
    private static readonly object FileLock = new();

    public void Append(string path, SegmentHistoryRecord record, int maxEntries)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = SegmentHistoryJson.ToJsonLine(record) + "\n";
        lock (FileLock)
        {
            File.AppendAllText(path, line);
            TrimToMax(path, maxEntries);
        }
    }

    public void TrimToMax(string path, int maxEntries)
    {
        var cap = SegmentHistoryMaxEntriesPolicy.Clamp(maxEntries);
        lock (FileLock)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var records = ReadValidRecords(path);
            if (records.Count <= cap)
            {
                return;
            }

            var kept = records.Skip(records.Count - cap).ToList();
            Rewrite(path, kept);
        }
    }

    public void Clear(string path)
    {
        lock (FileLock)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public IReadOnlyList<SegmentHistoryRecord> ReadAllNewestFirst(string path)
    {
        lock (FileLock)
        {
            if (!File.Exists(path))
            {
                return [];
            }

            var records = ReadValidRecords(path);
            records.Reverse();
            return records;
        }
    }

    private static List<SegmentHistoryRecord> ReadValidRecords(string path)
    {
        var records = new List<SegmentHistoryRecord>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (SegmentHistoryJson.TryParseLine(line, out var record) && record is not null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private static void Rewrite(string path, IReadOnlyList<SegmentHistoryRecord> records)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        var tempPath = Path.Combine(directory, $"{Path.GetFileName(path)}.tmp");
        using (var writer = new StreamWriter(tempPath, append: false))
        {
            foreach (var record in records)
            {
                writer.WriteLine(SegmentHistoryJson.ToJsonLine(record));
            }
        }

        File.Move(tempPath, path, overwrite: true);
    }
}
