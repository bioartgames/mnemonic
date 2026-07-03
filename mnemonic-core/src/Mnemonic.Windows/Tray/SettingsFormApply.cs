using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;

namespace Mnemonic.Windows.Tray;

internal static class SettingsFormApply
{
    public static void ApplyTrayFieldsFromUi(AppSettings target, AppSettings fromUi)
    {
        AppSettingsCaptureBinding.ApplyCaptureFields(target, fromUi);
        target.DrawMouse = fromUi.DrawMouse;
        target.SegmentDurationSeconds = fromUi.SegmentDurationSeconds;
        target.PreserveThreshold = fromUi.PreserveThreshold;
        target.NotableScoreMin = fromUi.NotableScoreMin;
        target.HighlightScoreMin = fromUi.HighlightScoreMin;
        target.SegmentHistoryMaxEntries = fromUi.SegmentHistoryMaxEntries;
    }
}
