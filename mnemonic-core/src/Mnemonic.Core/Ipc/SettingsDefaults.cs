using Mnemonic.Ipc.Models;
using Mnemonic.Retention;

namespace Mnemonic.Ipc;

public static class SettingsDefaults
{
    public static AppSettings Create() => new()
    {
        PreserveThreshold = 10,
        HighlightScoreMin = MnemonicConstants.SignificanceTierHighlightScoreMin,
        NotableScoreMin = 10,
        SegmentHistoryMaxEntries = SegmentHistoryMaxEntriesPolicy.Default,
        ScratchCapGb = 8,
        MicDeviceId = "",
        DesktopLoopbackDeviceId = "",
        CaptureMicEnabled = true,
        CaptureDesktopEnabled = true,
        DrawMouse = true,
        StartRecordingOnLaunch = true,
        SegmentDurationSeconds = global::Mnemonic.MnemonicConstants.SegmentDurationSeconds,
        FfmpegPathOverride = "",
    };
}
