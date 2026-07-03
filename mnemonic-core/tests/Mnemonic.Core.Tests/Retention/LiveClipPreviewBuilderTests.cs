using Mnemonic.Events;
using Mnemonic.Ipc;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class LiveClipPreviewBuilderTests
{
    [Fact]
    public void Build_returns_null_when_not_recording()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            File.WriteAllText(paths.SessionEventsFile, "{\"t\":100,\"type\":\"scene_save\"}\n");
            var settings = SettingsDefaults.Create();
            var ingest = new EventIngestService(paths, settings);
            ingest.Poll();

            var builder = new LiveClipPreviewBuilder(
                () => false,
                () => (true, 100.0, 220.0),
                () => 3,
                () => "mn_1_abcd",
                ingest,
                () => new GitSnapshot("abc", "main", "Subject"),
                settings);

            Assert.Null(builder.Build());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Build_populates_live_preview_fields_from_window_events_and_git_snapshot()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            File.WriteAllText(
                paths.SessionEventsFile,
                """
                {"t":110,"type":"scene_save","path":"res://combat/arena.tscn"}
                {"t":120,"type":"git_commit"}
                {"t":130,"type":"scene_transition","to_scene":"res://ui/menu.tscn"}
                """);
            var settings = SettingsDefaults.Create();
            var ingest = new EventIngestService(paths, settings);
            ingest.Poll();

            var builder = new LiveClipPreviewBuilder(
                () => true,
                () => (true, 100.0, 220.0),
                () => 7,
                () => "mn_1_abcd",
                ingest,
                () => new GitSnapshot("deadbeef", "feature/live-row", "Demo subject"),
                settings);

            var preview = builder.Build();

            Assert.NotNull(preview);
            Assert.Equal("mn_1_abcd", preview!.CapturePrefix);
            Assert.Equal(7, preview.SegmentIndex);
            Assert.Equal("mn_1_abcd_segment_00007", preview.SegmentId);
            Assert.Equal("feature/live-row", preview.GitBranch);
            Assert.Equal("Demo subject", preview.CommitSubject);
            Assert.Equal("deadbeef", preview.GitCommit);
            Assert.Contains("scene_save", preview.SignalTypes);
            Assert.Contains("git_commit", preview.SignalTypes);
            Assert.NotEmpty(preview.ScenesActive);
            Assert.NotEmpty(preview.Tags);
            Assert.True(preview.ScorePreview > 0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_live_preview_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "events"));
        return root;
    }
}
