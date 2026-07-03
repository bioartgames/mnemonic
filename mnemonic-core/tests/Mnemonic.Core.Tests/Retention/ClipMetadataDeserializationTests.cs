using Mnemonic.Ipc;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ClipMetadataDeserializationTests
{
    [Fact]
    public void Deserialize_legacy_clip_json_without_ai_fields()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_meta_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "clip.json");
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "id": "segment_00001",
                  "created_at": 220,
                  "duration_seconds": 120,
                  "score": 14,
                  "git_commit": "abc123",
                  "git_branch": "main",
                  "commit_subject": "Fix bug",
                  "scenes_active": [],
                  "tags": ["commit"],
                  "notes": ""
                }
                """);

            var meta = AtomicJsonFile.Read<ClipMetadata>(path, JsonOptions.Shared);

            Assert.NotNull(meta);
            Assert.Equal("segment_00001", meta!.Id);
            Assert.Null(meta.AiSummary);
            Assert.Null(meta.AiTopics);
            Assert.Null(meta.FilesModified);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Deserialize_includes_files_modified_when_present()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_meta_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "clip.json");
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "id": "segment_00001",
                  "created_at": 220,
                  "duration_seconds": 120,
                  "score": 14,
                  "git_commit": "abc123",
                  "git_branch": "main",
                  "commit_subject": "Fix bug",
                  "files_modified": ["src/a.cs"],
                  "scenes_active": [],
                  "tags": ["commit"],
                  "notes": ""
                }
                """);

            var meta = AtomicJsonFile.Read<ClipMetadata>(path, JsonOptions.Shared);

            Assert.NotNull(meta);
            Assert.NotNull(meta!.FilesModified);
            Assert.Equal(["src/a.cs"], meta.FilesModified);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
