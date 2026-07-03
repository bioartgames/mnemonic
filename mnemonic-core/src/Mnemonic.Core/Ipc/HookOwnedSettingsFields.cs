using Mnemonic.Ipc.Models;

namespace Mnemonic.Ipc;

internal static class HookOwnedSettingsFields
{
    public static void CopyFrom(AppSettings source, AppSettings target)
    {
        target.Heuristics = source.Heuristics;
        target.PreserveThreshold = source.PreserveThreshold;
        target.NotableScoreMin = source.NotableScoreMin;
        target.HighlightScoreMin = source.HighlightScoreMin;
        target.SegmentDurationSeconds = source.SegmentDurationSeconds;
        target.SegmentHistoryMaxEntries = source.SegmentHistoryMaxEntries;
        target.DrawMouse = source.DrawMouse;
        target.StartRecordingOnLaunch = source.StartRecordingOnLaunch;
    }

    /// <summary>
    /// Hook-exclusive fields preserved on tray save when Core in-memory state is stale.
    /// Tray-edited retention fields are not copied (see SettingsFormApply).
    /// </summary>
    public static void CopyTraySaveMergeFields(AppSettings source, AppSettings target)
    {
        target.Heuristics = source.Heuristics;
        target.StartRecordingOnLaunch = source.StartRecordingOnLaunch;
    }
}
