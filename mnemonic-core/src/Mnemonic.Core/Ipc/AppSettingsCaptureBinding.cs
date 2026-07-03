using Mnemonic.Ipc.Models;

namespace Mnemonic.Ipc;

public static class AppSettingsCaptureBinding
{
    public static AppSettings Snapshot(AppSettings source)
    {
        return new AppSettings
        {
            CaptureMicEnabled = source.CaptureMicEnabled,
            MicDeviceId = source.MicDeviceId,
            CaptureDesktopEnabled = source.CaptureDesktopEnabled,
            DesktopLoopbackDeviceId = source.DesktopLoopbackDeviceId,
            DrawMouse = source.DrawMouse,
            SegmentDurationSeconds = source.SegmentDurationSeconds,
        };
    }

    public static bool CaptureFieldsEqual(AppSettings a, AppSettings b)
    {
        return a.CaptureMicEnabled == b.CaptureMicEnabled
            && string.Equals(a.MicDeviceId, b.MicDeviceId, StringComparison.Ordinal)
            && a.CaptureDesktopEnabled == b.CaptureDesktopEnabled
            && string.Equals(a.DesktopLoopbackDeviceId, b.DesktopLoopbackDeviceId, StringComparison.Ordinal)
            && a.DrawMouse == b.DrawMouse
            && a.SegmentDurationSeconds == b.SegmentDurationSeconds;
    }

    public static void ApplyCaptureFields(AppSettings target, AppSettings source)
    {
        target.CaptureMicEnabled = source.CaptureMicEnabled;
        target.MicDeviceId = source.MicDeviceId;
        target.CaptureDesktopEnabled = source.CaptureDesktopEnabled;
        target.DesktopLoopbackDeviceId = source.DesktopLoopbackDeviceId;
        target.DrawMouse = source.DrawMouse;
        target.SegmentDurationSeconds = source.SegmentDurationSeconds;
    }
}
