using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests;

public sealed class CaptureArgvBuilderTests
{
    private const string Prefix = "mn_123_abc1";
    private const string OutPattern = @"C:\scratch\mn_123_abc1_segment_%05d.mp4";

    [Fact]
    public void Build_video_only_uses_gfxcapture_and_no_audio_inputs()
    {
        var argv = CaptureArgvBuilder.Build(CaptureAudioConfig.Empty(), true, 120, OutPattern, Prefix);
        var joined = string.Join(' ', argv);

        Assert.Contains("gfxcapture=monitor_idx=0", joined);
        Assert.Contains("capture_cursor=1", joined);
        Assert.Contains("[vout]", joined);
        Assert.Contains("-r 30", joined);
        Assert.Contains("-pix_fmt yuv420p", joined);
        Assert.Contains("-an", joined);
        Assert.Contains("-segment_format_options movflags=frag_keyframe+empty_moov+default_base_moof", joined);
        Assert.DoesNotContain("ddagrab", joined);
        Assert.DoesNotContain("dshow", joined);
        Assert.DoesNotContain(@"\\.\pipe\", joined);
    }

    [Fact]
    public void Build_mic_only_maps_first_audio_stream()
    {
        var config = new CaptureAudioConfig { MicDeviceId = "mic-endpoint-id" };
        var argv = CaptureArgvBuilder.Build(config, true, 120, OutPattern, Prefix);
        var joined = string.Join(' ', argv);

        Assert.Contains(@"\\.\pipe\mnemonic_" + Prefix + "_mic", joined);
        Assert.Contains("-map 0:a", joined);
        Assert.Contains("-c:a aac", joined);
        Assert.DoesNotContain("-map 1:a", joined);
        Assert.DoesNotContain("_desktop", joined);
    }

    [Fact]
    public void Build_desktop_only_maps_first_audio_stream()
    {
        var config = new CaptureAudioConfig { DesktopLoopbackDeviceId = "render-endpoint-id" };
        var argv = CaptureArgvBuilder.Build(config, true, 120, OutPattern, Prefix);
        var joined = string.Join(' ', argv);

        Assert.Contains(@"\\.\pipe\mnemonic_" + Prefix + "_desktop", joined);
        Assert.Contains("-map 0:a", joined);
        Assert.Contains("-c:a aac", joined);
        Assert.DoesNotContain("-map 1:a", joined);
    }

    [Fact]
    public void Build_both_maps_two_audio_streams_in_order()
    {
        var config = new CaptureAudioConfig
        {
            MicDeviceId = "mic-id",
            DesktopLoopbackDeviceId = "render-id",
        };
        var argv = CaptureArgvBuilder.Build(config, true, 120, OutPattern, Prefix);
        var joined = string.Join(' ', argv);

        Assert.Contains(@"\\.\pipe\mnemonic_" + Prefix + "_mic", joined);
        Assert.Contains(@"\\.\pipe\mnemonic_" + Prefix + "_desktop", joined);
        Assert.Contains("-map 0:a", joined);
        Assert.Contains("-map 1:a", joined);
        Assert.Contains("-c:a:0 aac", joined);
        Assert.Contains("-c:a:1 aac", joined);

        var micPos = joined.IndexOf("_mic", StringComparison.Ordinal);
        var deskPos = joined.IndexOf("_desktop", StringComparison.Ordinal);
        Assert.True(micPos >= 0 && deskPos >= 0 && micPos < deskPos);
    }

    [Fact]
    public void Build_draw_mouse_off_in_filter()
    {
        var argv = CaptureArgvBuilder.Build(CaptureAudioConfig.Empty(), false, 120, OutPattern, Prefix);
        var joined = string.Join(' ', argv);

        Assert.Contains("capture_cursor=0", joined);
    }

    [Fact]
    public void Build_segment_time_90()
    {
        var argv = CaptureArgvBuilder.Build(CaptureAudioConfig.Empty(), true, 90, OutPattern, Prefix);
        var joined = string.Join(' ', argv);

        Assert.Contains("-segment_time 90", joined);
    }
}
