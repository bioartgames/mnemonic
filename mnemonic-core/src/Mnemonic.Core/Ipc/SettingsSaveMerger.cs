using Mnemonic.Ipc.Models;

namespace Mnemonic.Ipc;

/// <summary>
/// Merges in-memory Core settings with on-disk settings before save so Hook
/// merge-writes (heuristics, retention thresholds) are not clobbered by stale Core state.
/// </summary>
internal static class SettingsSaveMerger
{
    public static AppSettings Merge(AppSettings? onDisk, AppSettings incoming)
    {
        if (onDisk is null)
        {
            return incoming;
        }

        HookOwnedSettingsFields.CopyFrom(onDisk, incoming);

        return incoming;
    }

    public static AppSettings MergeForTray(AppSettings? onDisk, AppSettings incoming)
    {
        if (onDisk is null)
        {
            return incoming;
        }

        HookOwnedSettingsFields.CopyTraySaveMergeFields(onDisk, incoming);

        return incoming;
    }
}
