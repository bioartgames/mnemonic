using System.Text.Json;
using Mnemonic;
using Mnemonic.Capture;
using Mnemonic.Ipc;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ClipIndexServiceTests
{
    [Fact]
    public void Rebuild_empty_clips_dir_writes_empty_index()
    {
        var root = CreateTempDataRoot();
        try
        {
            var paths = new DataRootPaths(root);
            new ClipIndexService(paths).Rebuild();

            Assert.True(File.Exists(paths.ClipsIndexFile));
            var index = AtomicJsonFile.Read<ClipIndexFile>(paths.ClipsIndexFile, JsonOptions.Shared);
            Assert.NotNull(index);
            Assert.Equal(MnemonicConstants.ClipsIndexVersion, index!.IndexVersion);
            Assert.Empty(index.Clips);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rebuild_single_clip()
    {
        var root = CreateTempDataRoot();
        try
        {
            var clipDir = Path.Combine(root, "clips", "mn_test_segment_00001");
            Directory.CreateDirectory(clipDir);
            var request = new ClipWriteRequest(
                "mn_test",
                1,
                0,
                120,
                120,
                5,
                [],
                CaptureAudioConfig.Empty(),
                GitSnapshot.Empty);
            ClipJsonWriter.Write(clipDir, request);
            File.WriteAllBytes(Path.Combine(clipDir, MnemonicConstants.ClipVideoFileName), [0, 1]);

            var paths = new DataRootPaths(root);
            new ClipIndexService(paths).Rebuild();

            var index = AtomicJsonFile.Read<ClipIndexFile>(paths.ClipsIndexFile, JsonOptions.Shared);
            Assert.NotNull(index);
            Assert.Single(index!.Clips);
            Assert.Equal("mn_test_segment_00001", index.Clips[0].Id);
            Assert.True(index.Clips[0].HasVideo);
            Assert.False(index.Clips[0].HasThumb);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rebuild_sorts_newest_first()
    {
        var root = CreateTempDataRoot();
        try
        {
            WriteClip(root, "mn_test", 1, createdAt: 100);
            WriteClip(root, "mn_test", 2, createdAt: 200);

            var paths = new DataRootPaths(root);
            new ClipIndexService(paths).Rebuild();

            var index = AtomicJsonFile.Read<ClipIndexFile>(paths.ClipsIndexFile, JsonOptions.Shared);
            Assert.NotNull(index);
            Assert.Equal(2, index!.Clips.Count);
            Assert.Equal("mn_test_segment_00002", index.Clips[0].Id);
            Assert.Equal("mn_test_segment_00001", index.Clips[1].Id);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rebuild_skips_folder_without_clip_json()
    {
        var root = CreateTempDataRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "clips", "segment_00099"));

            var paths = new DataRootPaths(root);
            new ClipIndexService(paths).Rebuild();

            var index = AtomicJsonFile.Read<ClipIndexFile>(paths.ClipsIndexFile, JsonOptions.Shared);
            Assert.NotNull(index);
            Assert.Empty(index!.Clips);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rebuild_skips_invalid_json()
    {
        var root = CreateTempDataRoot();
        try
        {
            var clipDir = Path.Combine(root, "clips", "segment_00001");
            Directory.CreateDirectory(clipDir);
            File.WriteAllText(Path.Combine(clipDir, "clip.json"), "not json");

            var paths = new DataRootPaths(root);
            new ClipIndexService(paths).Rebuild();

            var index = AtomicJsonFile.Read<ClipIndexFile>(paths.ClipsIndexFile, JsonOptions.Shared);
            Assert.NotNull(index);
            Assert.Empty(index!.Clips);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rebuild_merges_scene_tags_into_index_tags()
    {
        var root = CreateTempDataRoot();
        try
        {
            var clipDir = Path.Combine(root, "clips", "segment_00001");
            Directory.CreateDirectory(clipDir);
            var metadata = new ClipMetadata
            {
                Id = "segment_00001",
                CreatedAt = 120,
                DurationSeconds = 90,
                Score = 1,
                GitCommit = "",
                GitBranch = "",
                CommitSubject = "",
                ScenesActive = ["res://scenes/combat/arena.tscn"],
                Tags = ["playtest"],
            };
            AtomicJsonFile.Write(Path.Combine(clipDir, "clip.json"), metadata, JsonOptions.Shared);

            var paths = new DataRootPaths(root);
            new ClipIndexService(paths).Rebuild();

            var index = AtomicJsonFile.Read<ClipIndexFile>(paths.ClipsIndexFile, JsonOptions.Shared);
            Assert.NotNull(index);
            Assert.Single(index!.Clips);
            var entry = index.Clips[0];
            Assert.Equal(["res://scenes/combat/arena.tscn"], entry.ScenesActive);
            Assert.Contains("combat", entry.Tags);
            Assert.Contains("arena", entry.Tags);
            Assert.Contains("playtest", entry.Tags);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rebuild_includes_ai_fields_when_present()
    {
        var root = CreateTempDataRoot();
        try
        {
            var clipDir = Path.Combine(root, "clips", "segment_00001");
            Directory.CreateDirectory(clipDir);
            var metadata = new ClipMetadata
            {
                Id = "segment_00001",
                CreatedAt = 120,
                DurationSeconds = 120,
                Score = 1,
                GitCommit = "",
                GitBranch = "",
                CommitSubject = "",
                ScenesActive = [],
                Tags = ["playtest"],
                AiSummary = "A short summary",
                AiTopics = ["combat", "ui"],
            };
            AtomicJsonFile.Write(Path.Combine(clipDir, "clip.json"), metadata, JsonOptions.Shared);

            var paths = new DataRootPaths(root);
            new ClipIndexService(paths).Rebuild();

            var index = AtomicJsonFile.Read<ClipIndexFile>(paths.ClipsIndexFile, JsonOptions.Shared);
            Assert.NotNull(index);
            Assert.Single(index!.Clips);
            Assert.Equal("A short summary", index.Clips[0].AiSummary);
            Assert.Equal(["combat", "ui"], index.Clips[0].AiTopics);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rebuild_writes_suggested_groups_file()
    {
        var root = CreateTempDataRoot();
        try
        {
            WriteClip(root, "mn_test", 1, createdAt: 200);

            var paths = new DataRootPaths(root);
            new ClipIndexService(paths).Rebuild();

            Assert.True(File.Exists(paths.SuggestedGroupsFile));
            var groups = AtomicJsonFile.Read<SuggestedGroupsFile>(paths.SuggestedGroupsFile, JsonOptions.Shared);
            Assert.NotNull(groups);
            Assert.Equal(MnemonicConstants.SuggestedGroupsVersion, groups!.GroupsVersion);
            Assert.NotEmpty(groups.Groups);
            Assert.Contains(groups.Groups, g => g.ClipIds.Contains("mn_test_segment_00001"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteClip(string root, string capturePrefix, int segmentIndex, int createdAt)
    {
        var clipFolder = ClipIdentity.FormatClipId(capturePrefix, segmentIndex);
        var clipDir = Path.Combine(root, "clips", clipFolder);
        Directory.CreateDirectory(clipDir);
        var request = new ClipWriteRequest(
            capturePrefix,
            segmentIndex,
            0,
            createdAt,
            120,
            0,
            [],
            CaptureAudioConfig.Empty(),
            GitSnapshot.Empty);
        ClipJsonWriter.Write(clipDir, request);
    }

    private static string CreateTempDataRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_clip_index_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "clips"));
        Directory.CreateDirectory(Path.Combine(root, "control"));
        return root;
    }
}
