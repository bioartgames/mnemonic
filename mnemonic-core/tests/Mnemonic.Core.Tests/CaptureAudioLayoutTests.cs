using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests;

public sealed class CaptureAudioLayoutTests
{
    [Fact]
    public void GetTrackDescriptors_none_returns_empty()
    {
        var tracks = CaptureAudioLayout.GetTrackDescriptors(CaptureAudioConfig.Empty());
        Assert.Empty(tracks);
    }

    [Fact]
    public void GetTrackDescriptors_mic_only_stream_index_1()
    {
        var config = new CaptureAudioConfig { MicDeviceId = "Mic" };
        var tracks = CaptureAudioLayout.GetTrackDescriptors(config);

        Assert.Single(tracks);
        Assert.Equal("mic", tracks[0].Role);
        Assert.Equal(1, tracks[0].StreamIndex);
        Assert.Equal("aac", tracks[0].Codec);
    }

    [Fact]
    public void GetTrackDescriptors_desktop_only_stream_index_1()
    {
        var config = new CaptureAudioConfig { DesktopLoopbackDeviceId = "Stereo Mix" };
        var tracks = CaptureAudioLayout.GetTrackDescriptors(config);

        Assert.Single(tracks);
        Assert.Equal("desktop", tracks[0].Role);
        Assert.Equal(1, tracks[0].StreamIndex);
    }

    [Fact]
    public void GetTrackDescriptors_both_assigns_stream_indices()
    {
        var config = new CaptureAudioConfig
        {
            MicDeviceId = "Mic",
            DesktopLoopbackDeviceId = "Stereo Mix",
        };
        var tracks = CaptureAudioLayout.GetTrackDescriptors(config);

        Assert.Equal(2, tracks.Count);
        Assert.Equal("mic", tracks[0].Role);
        Assert.Equal(1, tracks[0].StreamIndex);
        Assert.Equal("desktop", tracks[1].Role);
        Assert.Equal(2, tracks[1].StreamIndex);
    }
}
