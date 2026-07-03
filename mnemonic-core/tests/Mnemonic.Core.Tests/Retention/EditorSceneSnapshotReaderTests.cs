using Mnemonic.Ipc;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class EditorSceneSnapshotReaderTests
{
    [Fact]
    public void TryRead_returns_empty_when_file_missing()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var result = EditorSceneSnapshotReader.TryRead(paths);
            Assert.Null(result.Edited);
            Assert.Null(result.Playing);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryRead_parses_valid_v1_snapshot()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.ControlDir);
            File.WriteAllText(
                paths.EditorSceneFile,
                """
                {
                  "contract_version": 1,
                  "updated_unix": 100.0,
                  "edited_scene_path": "res://edited.tscn",
                  "playing_scene_path": "res://playing.tscn"
                }
                """);

            var result = EditorSceneSnapshotReader.TryRead(paths);

            Assert.Equal("res://edited.tscn", result.Edited);
            Assert.Equal("res://playing.tscn", result.Playing);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryRead_returns_empty_for_wrong_contract_version()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.ControlDir);
            File.WriteAllText(
                paths.EditorSceneFile,
                """
                {
                  "contract_version": 99,
                  "edited_scene_path": "res://edited.tscn"
                }
                """);

            var result = EditorSceneSnapshotReader.TryRead(paths);

            Assert.Null(result.Edited);
            Assert.Null(result.Playing);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryRead_returns_empty_for_malformed_json()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.ControlDir);
            File.WriteAllText(paths.EditorSceneFile, "not json");

            var result = EditorSceneSnapshotReader.TryRead(paths);

            Assert.Null(result.Edited);
            Assert.Null(result.Playing);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_editor_scene_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }
}
