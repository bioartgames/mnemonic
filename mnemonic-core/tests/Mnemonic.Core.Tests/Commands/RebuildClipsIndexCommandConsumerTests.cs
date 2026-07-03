using Mnemonic;
using Mnemonic.Commands;
using Mnemonic.Ipc;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Commands;

public sealed class RebuildClipsIndexCommandConsumerTests
{
    [Fact]
    public void TryConsume_returns_false_when_missing()
    {
        var root = CreateTempDataRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var consumer = new RebuildClipsIndexCommandConsumer(paths, new ClipIndexService(paths));
            Assert.False(consumer.TryConsume());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryConsume_rebuilds_index_and_deletes_command()
    {
        var root = CreateTempDataRoot();
        try
        {
            var clipDir = Path.Combine(root, "clips", "segment_00001");
            Directory.CreateDirectory(clipDir);
            File.WriteAllText(
                Path.Combine(clipDir, "clip.json"),
                """
                {
                  "id": "segment_00001",
                  "created_at": 200,
                  "duration_seconds": 120,
                  "score": 5,
                  "git_commit": "",
                  "git_branch": "main",
                  "commit_subject": "Fix capture",
                  "scenes_active": [],
                  "tags": ["commit"],
                  "notes": ""
                }
                """);

            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.CommandsDir);
            File.WriteAllText(paths.RebuildClipsIndexFile, "{}");

            var consumer = new RebuildClipsIndexCommandConsumer(paths, new ClipIndexService(paths));
            Assert.True(consumer.TryConsume());
            Assert.False(File.Exists(paths.RebuildClipsIndexFile));
            Assert.True(File.Exists(paths.ClipsIndexFile));

            var index = AtomicJsonFile.Read<ClipIndexFile>(paths.ClipsIndexFile, JsonOptions.Shared);
            Assert.NotNull(index);
            Assert.Single(index!.Clips);
            Assert.Equal("segment_00001", index.Clips[0].Id);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryConsume_rebuild_failure_deletes_command()
    {
        var root = CreateTempDataRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.CommandsDir);
            File.WriteAllText(paths.RebuildClipsIndexFile, "{}");
            Directory.CreateDirectory(paths.ControlDir);
            File.WriteAllText(paths.ClipsIndexFile, "{}");
            File.SetAttributes(paths.ClipsIndexFile, FileAttributes.ReadOnly);

            var consumer = new RebuildClipsIndexCommandConsumer(paths, new ClipIndexService(paths));
            Assert.False(consumer.TryConsume());
            Assert.False(File.Exists(paths.RebuildClipsIndexFile));
        }
        finally
        {
            var indexFile = Path.Combine(root, "control", "clips_index.json");
            if (File.Exists(indexFile))
            {
                File.SetAttributes(indexFile, FileAttributes.Normal);
            }

            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDataRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_rebuild_cmd_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "clips"));
        Directory.CreateDirectory(Path.Combine(root, "control", "commands"));
        return root;
    }
}
