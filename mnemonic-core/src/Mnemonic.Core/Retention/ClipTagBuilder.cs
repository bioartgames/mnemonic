using Mnemonic.Events;

namespace Mnemonic.Retention;

public static class ClipTagBuilder
{
    private const int MaxBranchSlugLength = 48;

    public static IReadOnlyList<string> BuildTags(
        IReadOnlyList<SessionEvent> entries,
        string branchString,
        IReadOnlyList<string> scenePaths)
    {
        var tagsSet = new HashSet<string>(StringComparer.Ordinal);

        var slug = SlugBranch(branchString);
        if (slug.Length > 0)
        {
            tagsSet.Add(slug);
        }

        foreach (var e in entries)
        {
            if (e.Type.StartsWith("playtest", StringComparison.Ordinal))
            {
                tagsSet.Add("playtest");
            }

            switch (e.Type)
            {
                case "scene_save":
                case "resource_saved":
                case "resource_burst":
                    tagsSet.Add("save");
                    break;
                case "git_commit":
                case "checkpoint_after_work":
                case "commit_after_playtest":
                    tagsSet.Add("commit");
                    if (e.Type == "commit_after_playtest")
                    {
                        tagsSet.Add("commit_after_playtest");
                    }
                    break;
                case "scene_transition":
                case "scene_hopping":
                    tagsSet.Add("transition");
                    break;
                case "edit_intensity":
                case "long_edit_span":
                    tagsSet.Add("edit");
                    break;
                case "script_focus":
                    tagsSet.Add("script");
                    tagsSet.Add("edit");
                    break;
                case "layout_focus":
                    tagsSet.Add("layout");
                    tagsSet.Add("transition");
                    break;
                case "runtime_error":
                    tagsSet.Add("error");
                    break;
                case "git_push":
                    tagsSet.Add("push");
                    break;
                case "debug_session_start":
                    tagsSet.Add("debug");
                    break;
                case "script_save":
                    tagsSet.Add("script");
                    tagsSet.Add("save");
                    break;
                case "editor_focused_session":
                    tagsSet.Add("focus");
                    break;
            }
        }

        foreach (var sceneTag in ClipSceneTagDeriver.DeriveTags(scenePaths))
        {
            tagsSet.Add(sceneTag);
        }

        return tagsSet.OrderBy(t => t, StringComparer.Ordinal).ToList();
    }

    public static string SlugBranch(string branch)
    {
        var sb = branch.Trim().ToLowerInvariant().Replace('/', '-');
        if (sb.Length > MaxBranchSlugLength)
        {
            sb = sb[..MaxBranchSlugLength];
        }

        return sb;
    }
}
