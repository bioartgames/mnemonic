using Mnemonic.Capture;
using Mnemonic.Events;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class EventIngestServiceTests
{
    [Fact]
    public void Poll_stores_raw_scene_save_in_window()
    {
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                "{\"t\":150,\"type\":\"scene_save\"}\n");

            var ingest = new EventIngestService(new DataRootPaths(root), SettingsDefaults.Create());
            ingest.Poll();

            var events = ingest.EventsBetween(100, 200);
            Assert.Single(events, e => e.Type == "scene_save");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ScoreWindow_sums_events_in_segment_window()
    {
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                "{\"t\":150,\"type\":\"scene_save\"}\n{\"t\":160,\"type\":\"git_commit\"}\n");

            var paths = new DataRootPaths(root);
            var ingest = new EventIngestService(paths, SettingsDefaults.Create());
            ingest.Poll();

            Assert.Equal(14, ingest.ScoreWindow(100, 200));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Poll_derives_rapid_and_long_playtest_events()
    {
        var root = CreateTempDataRoot();
        try
        {
            var jsonl = string.Join(
                '\n',
                "{\"t\":1000,\"type\":\"playtest_start\"}",
                "{\"t\":1100,\"type\":\"playtest_start\"}",
                "{\"t\":1200,\"type\":\"playtest_start\"}",
                "{\"t\":2000,\"type\":\"playtest_stop\",\"duration_sec\":200}") + "\n";
            File.WriteAllText(Path.Combine(root, "events", "session_events.jsonl"), jsonl);

            var ingest = new EventIngestService(new DataRootPaths(root), SettingsDefaults.Create());
            ingest.Poll();

            // 3 starts (21) + rapid (9) + stop (0) + long (8) = 38
            Assert.Equal(38, ingest.ScoreWindow(0, 3000));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ScoreWindow_iteration_and_commit_derived()
    {
        var root = CreateTempDataRoot();
        try
        {
            var jsonl = string.Join(
                '\n',
                "{\"t\":150,\"type\":\"scene_save\",\"path\":\"res://a.tscn\"}",
                "{\"t\":160,\"type\":\"playtest_start\"}",
                "{\"t\":165,\"type\":\"playtest_stop\",\"duration_sec\":10}",
                "{\"t\":170,\"type\":\"git_commit\"}") + "\n";
            File.WriteAllText(Path.Combine(root, "events", "session_events.jsonl"), jsonl);

            var ingest = new EventIngestService(new DataRootPaths(root), SettingsDefaults.Create());
            ingest.Poll();

            Assert.Equal(41, ingest.ScoreWindow(100, 200));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Poll_stores_runtime_error_in_window()
    {
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                "{\"t\":150,\"type\":\"runtime_error\",\"message\":\"Invalid operands\"}\n");

            var ingest = new EventIngestService(new DataRootPaths(root), SettingsDefaults.Create());
            ingest.Poll();

            var events = ingest.EventsBetween(100, 200);
            Assert.Single(events, e => e.Type == "runtime_error");
            Assert.Equal(9, ingest.ScoreWindow(100, 200));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void OnSegmentClosed_flushes_partial_packet()
    {
        var root = CreateTempDataRoot();
        try
        {
            var jsonl = string.Join(
                '\n',
                "{\"t\":190,\"type\":\"resource_saved\",\"path\":\"res://a.gd\"}",
                "{\"t\":191,\"type\":\"resource_saved\",\"path\":\"res://b.gd\"}",
                "{\"t\":192,\"type\":\"resource_saved\",\"path\":\"res://c.gd\"}",
                "{\"t\":193,\"type\":\"resource_saved\",\"path\":\"res://d.gd\"}") + "\n";
            File.WriteAllText(Path.Combine(root, "events", "session_events.jsonl"), jsonl);

            var ingest = new EventIngestService(new DataRootPaths(root), SettingsDefaults.Create());
            ingest.OnSegmentClosed(new SegmentClosedEventArgs
            {
                CapturePrefix = "mn_test",
                Index = 1,
                ScratchPath = @"C:\scratch\mn_test_segment_00001.mp4",
                TOpenUnix = 100,
                TCloseUnix = 200,
            });

            var (_, lines) = ingest.ScoreBreakdownWindow(100, 200);
            Assert.Contains(lines, l => l.Type == "edit_intensity" && l.Points == 8);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void OnSegmentClosed_updates_last_segment_score()
    {
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                "{\"t\":150,\"type\":\"scene_save\"}\n");

            var ingest = new EventIngestService(new DataRootPaths(root), SettingsDefaults.Create());
            ingest.OnSegmentClosed(new SegmentClosedEventArgs
            {
                CapturePrefix = "mn_test",
                Index = 3,
                ScratchPath = @"C:\scratch\mn_test_segment_00003.mp4",
                TOpenUnix = 100,
                TCloseUnix = 200,
            });

            Assert.Equal(3, ingest.LastSegmentIndex);
            Assert.Equal(5, ingest.LastSegmentScore);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDataRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_ingest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "events"));
        return root;
    }
}
