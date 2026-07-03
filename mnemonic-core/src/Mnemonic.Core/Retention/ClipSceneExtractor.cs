using System.Text.Json;
using Mnemonic.Events;

namespace Mnemonic.Retention;

public readonly record struct EditorScenePaths(string? Edited, string? Playing);

public static class ClipSceneExtractor
{
    public static IReadOnlyList<string> BuildScenesActive(
        IReadOnlyList<SessionEvent> entries,
        EditorScenePaths editorPaths = default)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var outList = new List<string>();

        foreach (var e in entries)
        {
            switch (e.Type)
            {
                case "scene_transition":
                    TryAdd(outList, seen, GetExtraString(e, "to_scene"));
                    break;
                case "scene_save":
                    TryAdd(outList, seen, GetExtraString(e, "path"));
                    break;
                case "playtest_start":
                    TryAdd(outList, seen, GetExtraString(e, "scene_path"));
                    break;
            }
        }

        TryAdd(outList, seen, editorPaths.Playing);
        TryAdd(outList, seen, editorPaths.Edited);

        return outList;
    }

    private static void TryAdd(List<string> outList, HashSet<string> seen, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var trimmed = path.Trim();
        if (seen.Add(trimmed))
        {
            outList.Add(trimmed);
        }
    }

    private static string? GetExtraString(SessionEvent e, string key)
    {
        if (e.Extra is null || !e.Extra.TryGetValue(key, out var el))
        {
            return null;
        }

        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }
}
