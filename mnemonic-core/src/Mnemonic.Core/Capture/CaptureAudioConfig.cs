using Mnemonic.Ipc.Models;

namespace Mnemonic.Capture;

public sealed class CaptureAudioConfig
{
    public bool CaptureMicEnabled { get; init; }
    public bool CaptureDesktopEnabled { get; init; }
    public string MicDeviceId { get; init; } = "";
    public string DesktopLoopbackDeviceId { get; init; } = "";

    public static CaptureAudioConfig FromSettings(AppSettings settings) => new()
    {
        CaptureMicEnabled = settings.CaptureMicEnabled,
        CaptureDesktopEnabled = settings.CaptureDesktopEnabled,
        MicDeviceId = settings.MicDeviceId?.Trim() ?? "",
        DesktopLoopbackDeviceId = settings.DesktopLoopbackDeviceId?.Trim() ?? "",
    };

    public static CaptureAudioConfig Empty() => new();

    public bool HasMic => CaptureMicEnabled || MicDeviceId.Length > 0;

    public bool HasDesktop => CaptureDesktopEnabled || DesktopLoopbackDeviceId.Length > 0;

    public bool HasAnyAudio => HasMic || HasDesktop;
}
