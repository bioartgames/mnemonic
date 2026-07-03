using Mnemonic;

namespace Mnemonic.Heuristic;

public static class HeuristicCatalog
{
    public static IReadOnlyList<HeuristicCatalogEntry> Entries { get; } =
    [
        new("playtest_start", "Playtest start", "Editor playtest session started", "playtest", 7, MnemonicConstants.ScoreCapPlaytestStart),
        new("playtest_ongoing", "Playtest ongoing", "Middle segment of a long play (no stop yet); kept even if disabled in settings", "playtest", 7, 1),
        new("playtest_stop", "Playtest stop", "Playtest session ended (no score)", "playtest", 0, 0),
        new("rapid_playtest", "Rapid playtest", "Several playtests in a short window", "playtest", 9, 0),
        new("long_playtest", "Long playtest", "Playtest ran longer than the long threshold", "playtest", 8, 0),
        new("runtime_error", "Runtime error", "Script/runtime failure during playtest", "playtest", 9, MnemonicConstants.ScoreCapRuntimeError),
        new("scene_save", "Scene save", "A scene was saved in the editor", "editor", 5, MnemonicConstants.ScoreCapSceneSave),
        new("scene_transition", "Scene transition", "Active scene changed in the editor", "editor", 4, MnemonicConstants.ScoreCapSceneTransition),
        new("save_burst", "Save burst", "Multiple distinct scenes saved in a window", "editor", 6, 0),
        new("iteration_cycle", "Iteration cycle", "Playtest soon after a scene save", "editor", 10, 0),
        new("git_commit", "Git commit", "Git commit recorded during the segment", "git", 9, MnemonicConstants.ScoreCapGitCommit),
        new("git_branch_change", "Git branch change", "Git branch changed during the segment", "git", 6, MnemonicConstants.ScoreCapGitBranchChange),
        new("git_push", "Git push", "Local commits were pushed to the upstream remote", "git", 8, MnemonicConstants.ScoreCapGitPush),
        new("debug_session_start", "Debug session start", "Debugger attached during a playtest", "playtest", 6, MnemonicConstants.ScoreCapDebugSessionStart),
        new("debug_session_stop", "Debug session stop", "Debugger session ended (no score)", "playtest", 0, 0),
        new("script_save", "Script save", "A script or shader file was saved in the editor", "editor", 5, MnemonicConstants.ScoreCapScriptSave),
        new("editor_focused_session", "Editor focused session", "Sustained focus on one editor screen without playtest", "editor", 8, MnemonicConstants.ScoreCapEditorFocusedSession),
        new("commit_after_playtest", "Commit after playtest", "Commit shortly after a playtest ended", "git", 10, 0),
        new("resource_saved", "Resource saved", "A non-scene project file was saved (script, data, etc.)", "editor", 4, MnemonicConstants.ScoreCapResourceSaved),
        new("editor_focus_changed", "Editor focus changed", "Main editor screen changed (feeds activity packets; not scored)", "editor", 0, 0),
        new("activity_packet", "Activity packet", "Segment-scaled rolling summary of editor activity (internal; not scored)", "editor", 0, 0),
        new("resource_burst", "Resource burst", "Several distinct non-scene files saved in a window", "editor", 6, 0),
        new("edit_intensity", "Edit intensity", "Sustained editor saves/transitions with little playtest", "editor", 8, MnemonicConstants.ScoreCapEditIntensity),
        new("scene_hopping", "Scene hopping", "Many scene changes without playtest in the window", "editor", 6, MnemonicConstants.ScoreCapSceneHopping),
        new("script_focus", "Script focus", "Script editor dominated the activity window with resource saves and little playtest", "editor", 7, MnemonicConstants.ScoreCapScriptFocus),
        new("layout_focus", "Layout focus", "2D or 3D editor dominated the window with scene transitions and no playtest", "editor", 6, MnemonicConstants.ScoreCapLayoutFocus),
        new("checkpoint_after_work", "Checkpoint after work", "Git commit soon after an edit-intensity window", "git", 9, 0),
        new("long_edit_span", "Long edit span", "Long segment of editor work without playtest", "editor", 7, MnemonicConstants.ScoreCapLongEditSpan),
    ];

    public static HeuristicCatalogEntry? TryGet(string type)
    {
        foreach (var entry in Entries)
        {
            if (string.Equals(entry.Type, type, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }
}
