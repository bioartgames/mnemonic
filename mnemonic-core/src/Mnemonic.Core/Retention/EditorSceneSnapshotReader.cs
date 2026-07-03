using System.Text.Json;
using Mnemonic.Ipc;

namespace Mnemonic.Retention;

public static class EditorSceneSnapshotReader
{
    public static EditorScenePaths TryRead(DataRootPaths paths)
    {
        try
        {
            var filePath = paths.EditorSceneFile;
            if (!File.Exists(filePath))
            {
                return default;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return default;
            }

            if (!root.TryGetProperty("contract_version", out var versionEl)
                || !versionEl.TryGetInt32(out var version)
                || version != MnemonicConstants.EditorSceneContractVersion)
            {
                return default;
            }

            var edited = ReadPath(root, "edited_scene_path");
            var playing = ReadPath(root, "playing_scene_path");
            return new EditorScenePaths(edited, playing);
        }
        catch
        {
            return default;
        }
    }

    private static string? ReadPath(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el) || el.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = el.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
