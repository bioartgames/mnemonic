using Mnemonic;
using Mnemonic.Capture;
using Mnemonic.Events;
using Mnemonic.Git;
using Mnemonic.Heuristic;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Mnemonic.Retention;
using System.Text.Json;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ClipPreserverTests
{
    private const string TestCapturePrefix = "mn_test";

    [Fact]
    public void Preserve_writes_video_and_clip_json_with_metadata()
    {
        RunPreserveTest(
            """
            {"t":150,"type":"scene_save","path":"x.tscn"}
            {"t":160,"type":"scene_transition","to_scene":"res://main.tscn"}
            {"t":170,"type":"git_commit"}

            """,
            segmentIndex: 2,
            preserveThreshold: 10,
            manualFlag: true,
            expectPreserved: true,
            expectedScore: 18);
    }

    [Fact]
    public void Heuristic_preserve_when_score_meets_threshold()
    {
        RunPreserveTest(
            """
            {"t":150,"type":"scene_save","path":"x.tscn"}
            {"t":160,"type":"scene_transition","to_scene":"res://main.tscn"}
            {"t":170,"type":"git_commit"}

            """,
            segmentIndex: 2,
            preserveThreshold: 10,
            manualFlag: false,
            expectPreserved: true,
            expectedScore: 18);
    }

    [Fact]
    public void Heuristic_discards_when_score_below_threshold()
    {
        RunPreserveTest(
            "{\"t\":150,\"type\":\"scene_save\",\"path\":\"x.tscn\"}\n",
            segmentIndex: 2,
            preserveThreshold: 10,
            manualFlag: false,
            expectPreserved: false,
            expectedScore: 5);
    }

    [Fact]
    public void Heuristic_preserve_save_playtest_without_commit()
    {
        RunPreserveTest(
            """
            {"t":150,"type":"scene_save","path":"res://a.tscn"}
            {"t":160,"type":"playtest_start"}
            {"t":165,"type":"playtest_stop","duration_sec":10}
            {"t":170,"type":"git_commit"}

            """,
            segmentIndex: 2,
            preserveThreshold: 10,
            manualFlag: false,
            expectPreserved: true,
            expectedScore: 41);
    }

    [Fact]
    public void Heuristic_preserve_at_exact_threshold()
    {
        RunPreserveTest(
            "{\"t\":150,\"type\":\"git_commit\"}\n",
            segmentIndex: 2,
            preserveThreshold: 9,
            manualFlag: false,
            expectPreserved: true,
            expectedScore: 9);
    }

    [Fact]
    public void Manual_flag_preserves_below_threshold()
    {
        RunPreserveTest(
            "{\"t\":150,\"type\":\"scene_save\",\"path\":\"x.tscn\"}\n",
            segmentIndex: 2,
            preserveThreshold: 10,
            manualFlag: true,
            expectPreserved: true,
            expectedScore: 5);
    }

    [Fact]
    public void Playtest_ongoing_preserves_middle_segment_below_threshold()
    {
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                "{\"t\":50,\"type\":\"playtest_start\"}\n");

            var paths = new DataRootPaths(root);
            const int segmentIndex = 2;
            var scratchPath = Path.Combine(root, "scratch", $"mn_test_segment_{segmentIndex:D5}.mp4");
            File.WriteAllBytes(scratchPath, [0, 1, 2, 3]);

            var settings = SettingsDefaults.Create();
            settings.PreserveThreshold = 10;
            var ingest = new EventIngestService(paths, settings);
            var gitPoll = new GitPollService(paths, new GitProcessRunner(), ingest, root);
            var clipIndex = new ClipIndexService(paths);
            var preserver = new ClipPreserver(
                paths,
                new ManualPreserveTracker(),
                ingest,
                gitPoll,
                settings,
                clipIndex,
                new StatusStore(paths));
            preserver.HandleSegmentClosed(new SegmentClosedEventArgs
            {
                CapturePrefix = TestCapturePrefix,
                Index = segmentIndex,
                ScratchPath = scratchPath,
                TOpenUnix = 100,
                TCloseUnix = 200,
            });

            var clipJsonPath = Path.Combine(
                root,
                "clips",
                $"{TestCapturePrefix}_segment_{segmentIndex:D5}",
                "clip.json");
            Assert.True(File.Exists(clipJsonPath));

            using var doc = JsonDocument.Parse(File.ReadAllText(clipJsonPath));
            Assert.Equal(7, doc.RootElement.GetProperty("score").GetInt32());
            var tags = doc.RootElement.GetProperty("tags").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.Contains("playtest", tags);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Preserve_Succeeds_WhenThumbnailSkipped()
    {
        RunPreserveTest(
            "{\"t\":150,\"type\":\"git_commit\"}\n",
            segmentIndex: 4,
            preserveThreshold: 9,
            manualFlag: false,
            expectPreserved: true,
            expectedScore: 9,
            ffmpeg: new FfmpegResolution(null, false, ""));
    }

    [Fact]
    public void Preserve_writes_files_modified_when_git_commit_and_runner_returns_paths()
    {
        var root = CreateTempDataRoot();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(repo);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                """
                {"t":150,"type":"git_commit","commit":"deadbeef","subject":"test"}

                """);

            var paths = new DataRootPaths(root);
            const int segmentIndex = 7;
            var scratchPath = Path.Combine(root, "scratch", $"mn_test_segment_{segmentIndex:D5}.mp4");
            File.WriteAllBytes(scratchPath, [0, 1, 2, 3]);

            var gitRunner = new FilesModifiedFakeGitCommandRunner();
            gitRunner.EnqueueFullOutputOk("src/foo.cs\nsrc/bar.cs\n");

            var settings = SettingsDefaults.Create();
            settings.PreserveThreshold = 9;
            var ingest = new EventIngestService(paths, settings);
            var gitPoll = new GitPollService(paths, gitRunner, ingest, repo);
            var fileListResolver = new GitCommitFileListResolver(gitRunner, repo);
            var clipIndex = new ClipIndexService(paths);
            var preserver = new ClipPreserver(
                paths,
                new ManualPreserveTracker(),
                ingest,
                gitPoll,
                settings,
                clipIndex,
                new StatusStore(paths),
                fileListResolver: fileListResolver);
            preserver.HandleSegmentClosed(new SegmentClosedEventArgs
            {
                CapturePrefix = TestCapturePrefix,
                Index = segmentIndex,
                ScratchPath = scratchPath,
                TOpenUnix = 100,
                TCloseUnix = 200,
            });

            var clipJsonPath = Path.Combine(
                root,
                "clips",
                $"{TestCapturePrefix}_segment_{segmentIndex:D5}",
                "clip.json");
            Assert.True(File.Exists(clipJsonPath));

            using var doc = JsonDocument.Parse(File.ReadAllText(clipJsonPath));
            var files = doc.RootElement.GetProperty("files_modified");
            Assert.Equal(2, files.GetArrayLength());
            Assert.Equal("src/foo.cs", files[0].GetString());
            Assert.Equal("src/bar.cs", files[1].GetString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Heuristic_discards_playtest_only_when_runtime_error_heuristic_disabled()
    {
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                """
                {"t":150,"type":"playtest_start"}
                {"t":155,"type":"runtime_error","message":"Invalid operands","scene":"res://main.gd","line":10}

                """);

            var paths = new DataRootPaths(root);
            const int segmentIndex = 2;
            var scratchPath = Path.Combine(root, "scratch", $"mn_test_segment_{segmentIndex:D5}.mp4");
            File.WriteAllBytes(scratchPath, [0, 1, 2, 3]);

            var settings = SettingsDefaults.Create();
            settings.PreserveThreshold = 10;
            settings.Heuristics = new Dictionary<string, HeuristicTypeSettings>
            {
                ["runtime_error"] = new() { Enabled = false, Weight = 9, Cap = 3 },
            };
            var ingest = new EventIngestService(paths, settings);
            var gitPoll = new GitPollService(paths, new GitProcessRunner(), ingest, root);
            var clipIndex = new ClipIndexService(paths);
            var preserver = new ClipPreserver(
                paths,
                new ManualPreserveTracker(),
                ingest,
                gitPoll,
                settings,
                clipIndex,
                new StatusStore(paths));
            preserver.HandleSegmentClosed(new SegmentClosedEventArgs
            {
                CapturePrefix = TestCapturePrefix,
                Index = segmentIndex,
                ScratchPath = scratchPath,
                TOpenUnix = 100,
                TCloseUnix = 200,
            });

            Assert.False(File.Exists(Path.Combine(root, "clips", $"{TestCapturePrefix}_segment_{segmentIndex:D5}", "clip.json")));
            Assert.False(File.Exists(scratchPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Heuristic_preserve_real_session_segment0_playtest_and_runtime_error()
    {
        const long captureStart = 1780205556L;
        const int segmentSeconds = 120;
        const int segmentIndex = 0;
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                """
                {"t":1780205567.143,"type":"playtest_start"}
                {"line":5,"message":"Invalid assignment","scene":"res://addons/mnemonic_hook/new_script.gd","t":1780205568.009,"type":"runtime_error"}
                {"duration_sec":3.625,"t":1780205570.768,"type":"playtest_stop"}

                """);

            var paths = new DataRootPaths(root);
            var scratchPath = Path.Combine(root, "scratch", $"mn_1780205556_4f74_segment_{segmentIndex:D5}.mp4");
            File.WriteAllBytes(scratchPath, [0, 1, 2, 3]);

            var settings = SettingsDefaults.Create();
            settings.PreserveThreshold = 10;
            var ingest = new EventIngestService(paths, settings);
            var gitPoll = new GitPollService(paths, new GitProcessRunner(), ingest, root);
            var clipIndex = new ClipIndexService(paths);
            var preserver = new ClipPreserver(
                paths,
                new ManualPreserveTracker(),
                ingest,
                gitPoll,
                settings,
                clipIndex,
                new StatusStore(paths));
            preserver.HandleSegmentClosed(new SegmentClosedEventArgs
            {
                CapturePrefix = "mn_1780205556_4f74",
                Index = segmentIndex,
                ScratchPath = scratchPath,
                TOpenUnix = captureStart,
                TCloseUnix = captureStart + segmentSeconds,
            });

            var clipJsonPath = Path.Combine(
                root,
                "clips",
                "mn_1780205556_4f74_segment_00000",
                "clip.json");
            Assert.True(File.Exists(clipJsonPath));

            using var doc = JsonDocument.Parse(File.ReadAllText(clipJsonPath));
            Assert.Equal(16, doc.RootElement.GetProperty("score").GetInt32());
            var tags = doc.RootElement.GetProperty("tags").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.Contains("error", tags);
            Assert.Contains("playtest", tags);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Heuristic_preserve_playtest_with_runtime_error()
    {
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                """
                {"t":150,"type":"playtest_start"}
                {"t":155,"type":"runtime_error","message":"Invalid operands","scene":"res://main.gd","line":10}

                """);

            var paths = new DataRootPaths(root);
            const int segmentIndex = 2;
            var scratchPath = Path.Combine(root, "scratch", $"mn_test_segment_{segmentIndex:D5}.mp4");
            File.WriteAllBytes(scratchPath, [0, 1, 2, 3]);

            var settings = SettingsDefaults.Create();
            settings.PreserveThreshold = 10;
            var ingest = new EventIngestService(paths, settings);
            settings.MicDeviceId = "Mic";

            var gitPoll = new GitPollService(paths, new GitProcessRunner(), ingest, root);
            var clipIndex = new ClipIndexService(paths);
            var preserver = new ClipPreserver(
                paths,
                new ManualPreserveTracker(),
                ingest,
                gitPoll,
                settings,
                clipIndex,
                new StatusStore(paths));
            preserver.HandleSegmentClosed(new SegmentClosedEventArgs
            {
                CapturePrefix = TestCapturePrefix,
                Index = segmentIndex,
                ScratchPath = scratchPath,
                TOpenUnix = 100,
                TCloseUnix = 200,
            });

            var clipJsonPath = Path.Combine(
                root,
                "clips",
                $"{TestCapturePrefix}_segment_{segmentIndex:D5}",
                "clip.json");
            Assert.True(File.Exists(clipJsonPath));

            using var doc = JsonDocument.Parse(File.ReadAllText(clipJsonPath));
            Assert.Equal(16, doc.RootElement.GetProperty("score").GetInt32());
            var tags = doc.RootElement.GetProperty("tags").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.Contains("error", tags);
            Assert.Contains("playtest", tags);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Heuristic_preserve_resource_edit_without_playtest()
    {
        RunPreserveTest(
            """
            {"t":130,"type":"resource_saved","path":"res://a.gd"}
            {"t":140,"type":"resource_saved","path":"res://b.gd"}
            {"t":150,"type":"resource_saved","path":"res://c.gd"}
            {"t":160,"type":"scene_save","path":"res://main.tscn"}
            {"t":170,"type":"scene_transition","to_scene":"res://hub.tscn"}
            {"t":180,"type":"resource_saved","path":"res://d.gd"}

            """,
            segmentIndex: 2,
            preserveThreshold: 10,
            manualFlag: false,
            expectPreserved: true,
            expectedScore: 38);
    }

    [Fact]
    public void Heuristic_preserve_scene_hopping_without_playtest()
    {
        RunPreserveTest(
            """
            {"t":110,"type":"scene_transition","to_scene":"res://a.tscn"}
            {"t":120,"type":"scene_transition","to_scene":"res://b.tscn"}
            {"t":130,"type":"scene_transition","to_scene":"res://c.tscn"}
            {"t":190,"type":"scene_save","path":"res://c.tscn"}

            """,
            segmentIndex: 2,
            preserveThreshold: 10,
            manualFlag: false,
            expectPreserved: true,
            expectedScore: 27);
    }

    [Fact]
    public void Heuristic_preserve_long_edit_span_synthetic()
    {
        RunPreserveTest(
            """
            {"t":110,"type":"scene_save","path":"res://a.tscn"}
            {"t":120,"type":"resource_saved","path":"res://a.gd"}
            {"t":130,"type":"scene_transition","to_scene":"res://b.tscn"}
            {"t":140,"type":"scene_save","path":"res://b.tscn"}
            {"t":150,"type":"resource_saved","path":"res://b.gd"}

            """,
            segmentIndex: 2,
            preserveThreshold: 10,
            manualFlag: false,
            expectPreserved: true,
            expectedScore: 29);
    }

    [Fact]
    public void Manual_flag_cleared_after_segment_close()
    {
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "events", "session_events.jsonl"),
                "{\"t\":150,\"type\":\"scene_save\"}\n");

            var paths = new DataRootPaths(root);
            var scratchPath = Path.Combine(root, "scratch", "mn_test_segment_00003.mp4");
            File.WriteAllBytes(scratchPath, [0, 1, 2, 3]);

            var tracker = new ManualPreserveTracker();
            tracker.RequestPreserve(3);
            var settings = SettingsDefaults.Create();
            var ingest = new EventIngestService(paths, settings);

            var gitPoll = new GitPollService(paths, new GitProcessRunner(), ingest, root);
            var clipIndex = new ClipIndexService(paths);
            var preserver = new ClipPreserver(
                paths,
                tracker,
                ingest,
                gitPoll,
                settings,
                clipIndex,
                new StatusStore(paths));
            preserver.HandleSegmentClosed(new SegmentClosedEventArgs
            {
                CapturePrefix = TestCapturePrefix,
                Index = 3,
                ScratchPath = scratchPath,
                TOpenUnix = 100,
                TCloseUnix = 200,
            });

            Assert.False(tracker.ShouldPreserve(3));
            Assert.True(File.Exists(Path.Combine(root, "clips", "mn_test_segment_00003", "video.mp4")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void RunPreserveTest(
        string jsonl,
        int segmentIndex,
        int preserveThreshold,
        bool manualFlag,
        bool expectPreserved,
        int expectedScore,
        FfmpegResolution? ffmpeg = null)
    {
        var root = CreateTempDataRoot();
        try
        {
            File.WriteAllText(Path.Combine(root, "events", "session_events.jsonl"), jsonl);

            var paths = new DataRootPaths(root);
            var scratchPath = Path.Combine(root, "scratch", $"mn_test_segment_{segmentIndex:D5}.mp4");
            File.WriteAllBytes(scratchPath, [0, 1, 2, 3]);

            var tracker = new ManualPreserveTracker();
            if (manualFlag)
            {
                tracker.RequestPreserve(segmentIndex);
            }

            var settings = SettingsDefaults.Create();
            settings.PreserveThreshold = preserveThreshold;
            var ingest = new EventIngestService(paths, settings);
            settings.MicDeviceId = "Mic";

            var gitPoll = new GitPollService(paths, new GitProcessRunner(), ingest, root);
            var clipIndex = new ClipIndexService(paths);
            var preserver = new ClipPreserver(
                paths,
                tracker,
                ingest,
                gitPoll,
                settings,
                clipIndex,
                new StatusStore(paths),
                ffmpeg);
            preserver.HandleSegmentClosed(new SegmentClosedEventArgs
            {
                CapturePrefix = TestCapturePrefix,
                Index = segmentIndex,
                ScratchPath = scratchPath,
                TOpenUnix = 100,
                TCloseUnix = 200,
            });

            var clipDir = Path.Combine(root, "clips", $"{TestCapturePrefix}_segment_{segmentIndex:D5}");
            if (expectPreserved)
            {
                Assert.False(File.Exists(scratchPath));
                Assert.True(File.Exists(Path.Combine(clipDir, "video.mp4")));
                Assert.True(File.Exists(Path.Combine(clipDir, "clip.json")));
                Assert.False(File.Exists(Path.Combine(clipDir, MnemonicConstants.ClipThumbFileName)));

                using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(clipDir, "clip.json")));
                Assert.Equal(expectedScore, doc.RootElement.GetProperty("score").GetInt32());

                Assert.True(File.Exists(paths.ClipsIndexFile));
                var index = AtomicJsonFile.Read<ClipIndexFile>(paths.ClipsIndexFile, JsonOptions.Shared);
                Assert.NotNull(index);
                Assert.Single(index!.Clips);
                Assert.Equal($"{TestCapturePrefix}_segment_{segmentIndex:D5}", index.Clips[0].Id);
                Assert.Equal(expectedScore, index.Clips[0].Score);
            }
            else
            {
                Assert.False(File.Exists(scratchPath));
                Assert.False(Directory.Exists(clipDir));
            }

            AssertSegmentHistory(paths, segmentIndex, expectPreserved, expectedScore, manualFlag);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void AssertSegmentHistory(
        DataRootPaths paths,
        int segmentIndex,
        bool expectPreserved,
        int expectedScore,
        bool manualFlag)
    {
        Assert.True(File.Exists(paths.SegmentHistoryFile));
        var store = new SegmentHistoryStore();
        var records = store.ReadAllNewestFirst(paths.SegmentHistoryFile);
        Assert.Single(records);
        var record = records[0];
        Assert.Equal(segmentIndex, record.SegmentIndex);
        Assert.Equal(TestCapturePrefix, record.CapturePrefix);
        Assert.Equal(expectedScore, record.Score);
        Assert.Equal(expectPreserved, record.Preserved);
        Assert.Equal(manualFlag, record.ManualPreserve);
        if (expectPreserved)
        {
            Assert.Equal($"{TestCapturePrefix}_segment_{segmentIndex:D5}", record.ClipId);
        }
        else
        {
            Assert.Equal("", record.ClipId);
        }
    }

    private static string CreateTempDataRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_preserver_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "scratch"));
        Directory.CreateDirectory(Path.Combine(root, "clips"));
        Directory.CreateDirectory(Path.Combine(root, "events"));
        Directory.CreateDirectory(Path.Combine(root, "control"));
        return root;
    }

    private sealed class FilesModifiedFakeGitCommandRunner : IGitCommandRunner
    {
        private readonly Queue<GitCommandResult> _fullOutput = new();

        public void EnqueueFullOutputOk(string stdout) =>
            _fullOutput.Enqueue(new GitCommandResult(true, 0, stdout));

        public GitCommandResult Run(string repoRoot, IReadOnlyList<string> args) =>
            new(false, 1, "");

        public GitCommandResult RunFullOutput(string repoRoot, IReadOnlyList<string> args) =>
            _fullOutput.Count > 0
                ? _fullOutput.Dequeue()
                : new GitCommandResult(false, 1, "");
    }
}
