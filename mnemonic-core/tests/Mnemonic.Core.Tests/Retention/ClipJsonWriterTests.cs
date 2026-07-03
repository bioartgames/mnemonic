using System.Text.Json;
using Mnemonic.Capture;
using Mnemonic.Events;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ClipJsonWriterTests
{
    private const string TestCapturePrefix = "mn_test";

    [Fact]
    public void Write_creates_expected_json_without_assets()
    {
        var dir = CreateTempClipDir();
        try
        {
            var request = new ClipWriteRequest(
                TestCapturePrefix,
                3,
                100,
                220,
                120,
                14,
                [],
                CaptureAudioConfig.Empty(),
                GitSnapshot.Empty);

            ClipJsonWriter.Write(dir, request);

            var json = File.ReadAllText(Path.Combine(dir, "clip.json"));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal("mn_test_segment_00003", root.GetProperty("id").GetString());
            Assert.Equal(220, root.GetProperty("created_at").GetInt32());
            Assert.Equal(120, root.GetProperty("duration_seconds").GetInt32());
            Assert.Equal(14, root.GetProperty("score").GetInt32());
            Assert.False(root.TryGetProperty("assets", out _));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_includes_git_snapshot_fields()
    {
        var dir = CreateTempClipDir();
        try
        {
            var request = new ClipWriteRequest(
                TestCapturePrefix,
                1,
                0,
                120,
                120,
                0,
                [],
                CaptureAudioConfig.Empty(),
                new GitSnapshot("abc123", "main", "Fix bug"));

            ClipJsonWriter.Write(dir, request);

            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "clip.json")));
            var root = doc.RootElement;
            Assert.Equal("abc123", root.GetProperty("git_commit").GetString());
            Assert.Equal("main", root.GetProperty("git_branch").GetString());
            Assert.Equal("Fix bug", root.GetProperty("commit_subject").GetString());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_uses_request_segment_duration()
    {
        var dir = CreateTempClipDir();
        try
        {
            var request = new ClipWriteRequest(
                TestCapturePrefix,
                2,
                0,
                90,
                90,
                0,
                [],
                CaptureAudioConfig.Empty(),
                GitSnapshot.Empty);

            ClipJsonWriter.Write(dir, request);

            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "clip.json")));
            Assert.Equal(90, doc.RootElement.GetProperty("duration_seconds").GetInt32());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_omits_ai_fields_by_default()
    {
        var dir = CreateTempClipDir();
        try
        {
            var request = new ClipWriteRequest(
                TestCapturePrefix,
                1,
                0,
                120,
                120,
                0,
                [],
                CaptureAudioConfig.Empty(),
                GitSnapshot.Empty);

            ClipJsonWriter.Write(dir, request);

            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "clip.json")));
            var root = doc.RootElement;
            Assert.False(root.TryGetProperty("ai_summary", out _));
            Assert.False(root.TryGetProperty("ai_topics", out _));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_includes_playtest_start_scene_path()
    {
        var dir = CreateTempClipDir();
        try
        {
            Assert.True(
                SessionEvent.TryParseFromJsonLine(
                    "{\"t\":1,\"type\":\"playtest_start\",\"scene_path\":\"res://main.tscn\"}",
                    out var start));

            var request = new ClipWriteRequest(
                TestCapturePrefix,
                0,
                0,
                120,
                120,
                5,
                [start!],
                CaptureAudioConfig.Empty(),
                GitSnapshot.Empty);

            ClipJsonWriter.Write(dir, request);

            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "clip.json")));
            var scenes = doc.RootElement.GetProperty("scenes_active");
            Assert.Equal(1, scenes.GetArrayLength());
            Assert.Equal("res://main.tscn", scenes[0].GetString());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_includes_files_modified_when_provided()
    {
        var dir = CreateTempClipDir();
        try
        {
            var request = new ClipWriteRequest(
                TestCapturePrefix,
                1,
                0,
                120,
                120,
                9,
                [],
                CaptureAudioConfig.Empty(),
                GitSnapshot.Empty,
                default,
                ["a.gd", "b.cs"]);

            ClipJsonWriter.Write(dir, request);

            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "clip.json")));
            var files = doc.RootElement.GetProperty("files_modified");
            Assert.Equal(2, files.GetArrayLength());
            Assert.Equal("a.gd", files[0].GetString());
            Assert.Equal("b.cs", files[1].GetString());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_omits_files_modified_when_empty()
    {
        var dir = CreateTempClipDir();
        try
        {
            var request = new ClipWriteRequest(
                TestCapturePrefix,
                1,
                0,
                120,
                120,
                0,
                [],
                CaptureAudioConfig.Empty(),
                GitSnapshot.Empty);

            ClipJsonWriter.Write(dir, request);

            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "clip.json")));
            Assert.False(doc.RootElement.TryGetProperty("files_modified", out _));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Write_includes_assets_when_audio_configured()
    {
        var dir = CreateTempClipDir();
        try
        {
            var config = new CaptureAudioConfig { MicDeviceId = "Mic", DesktopLoopbackDeviceId = "Desktop" };
            var request = new ClipWriteRequest(TestCapturePrefix, 0, 0, 120, 120, 0, [], config, GitSnapshot.Empty);

            ClipJsonWriter.Write(dir, request);

            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "clip.json")));
            var assets = doc.RootElement.GetProperty("assets");
            Assert.Equal("video.mp4", assets.GetProperty("container").GetString());
            Assert.Equal(2, assets.GetProperty("audio_tracks").GetArrayLength());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempClipDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_clip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
