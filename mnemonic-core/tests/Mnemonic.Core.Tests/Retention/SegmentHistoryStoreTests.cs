using Mnemonic.Heuristic;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class SegmentHistoryStoreTests
{
    [Fact]
    public void Append_and_read_round_trip()
    {
        var path = CreateTempPath();
        try
        {
            var store = new SegmentHistoryStore();
            var record = SampleRecord(segmentIndex: 1, score: 5);
            store.Append(path, record, maxEntries: 10);

            var read = store.ReadAllNewestFirst(path);
            Assert.Single(read);
            Assert.Equal(1, read[0].SegmentIndex);
            Assert.Equal(5, read[0].Score);
        }
        finally
        {
            DeletePath(path);
        }
    }

    [Fact]
    public void Json_round_trip_preserves_breakdown_detail()
    {
        var record = SampleRecord(7, 12);
        record.Breakdown =
        [
            new() { Type = "edit_intensity", Count = 1, Points = 8, Detail = "2 scene, 2 resource, 0 transitions, 0s playtest in 60s" },
        ];
        Assert.True(SegmentHistoryJson.TryParseLine(SegmentHistoryJson.ToJsonLine(record), out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal("2 scene, 2 resource, 0 transitions, 0s playtest in 60s", parsed!.Breakdown[0].Detail);
    }

    [Fact]
    public void Json_round_trip()
    {
        var record = SampleRecord(7, 12);
        Assert.True(SegmentHistoryJson.TryParseLine(SegmentHistoryJson.ToJsonLine(record), out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal(7, parsed!.SegmentIndex);
        Assert.Equal(12, parsed.Score);
    }

    [Fact]
    public void Trim_keeps_last_n_entries()
    {
        var path = CreateTempPath();
        try
        {
            var store = new SegmentHistoryStore();
            for (var i = 0; i < 15; i++)
            {
                store.Append(path, SampleRecord(segmentIndex: i, score: i), maxEntries: 1000);
            }

            store.TrimToMax(path, 10);
            var read = store.ReadAllNewestFirst(path);
            Assert.Equal(10, read.Count);
            Assert.Equal(14, read[0].SegmentIndex);
            Assert.Equal(5, read[9].SegmentIndex);
        }
        finally
        {
            DeletePath(path);
        }
    }

    [Fact]
    public void Clear_removes_file()
    {
        var path = CreateTempPath();
        try
        {
            var store = new SegmentHistoryStore();
            store.Append(path, SampleRecord(0, 1), maxEntries: 10);
            store.Clear(path);
            Assert.False(File.Exists(path));
        }
        finally
        {
            DeletePath(path);
        }
    }

    [Fact]
    public void Read_skips_invalid_lines()
    {
        var path = CreateTempPath();
        try
        {
            File.WriteAllText(
                path,
                """
                not json
                {"contract_version":1,"segment_index":1,"capture_prefix":"p","clip_id":"","t_open_unix":1,"t_close_unix":2,"segment_duration_seconds":120,"score":3,"threshold":10,"preserved":false,"manual_preserve":false,"breakdown":[],"git_branch":"","git_commit":"","git_subject":""}

                """);

            var store = new SegmentHistoryStore();
            var read = store.ReadAllNewestFirst(path);
            Assert.Single(read);
            Assert.Equal(1, read[0].SegmentIndex);
        }
        finally
        {
            DeletePath(path);
        }
    }

    private static SegmentHistoryRecord SampleRecord(int segmentIndex, int score) =>
        new()
        {
            SegmentIndex = segmentIndex,
            CapturePrefix = "test",
            Score = score,
            Threshold = 10,
            SegmentDurationSeconds = 120,
            TOpenUnix = 100,
            TCloseUnix = 200,
        };

    private static string CreateTempPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_seg_hist_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "segment_history.jsonl");
    }

    private static void DeletePath(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
