using Mnemonic.Capture;
using Mnemonic.Ipc;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class SettingsDefaultsTests
{
    [Fact]
    public void Create_sets_start_recording_on_launch_true()
    {
        var settings = SettingsDefaults.Create();
        Assert.True(settings.StartRecordingOnLaunch);
    }

    [Fact]
    public void Create_enables_mic_and_desktop_with_empty_device_ids()
    {
        var settings = SettingsDefaults.Create();

        Assert.True(settings.CaptureMicEnabled);
        Assert.True(settings.CaptureDesktopEnabled);
        Assert.Equal("", settings.MicDeviceId);
        Assert.Equal("", settings.DesktopLoopbackDeviceId);
    }

    [Fact]
    public void CaptureAudioConfig_FromSettings_has_any_audio_true_on_defaults()
    {
        var settings = SettingsDefaults.Create();
        var config = CaptureAudioConfig.FromSettings(settings);

        Assert.True(config.HasAnyAudio);
    }
}
