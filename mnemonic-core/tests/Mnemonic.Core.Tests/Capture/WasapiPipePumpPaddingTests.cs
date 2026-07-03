using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class WasapiPipePumpPaddingTests
{
    [Fact]
    public void ShouldWriteSilence_false_before_pipe_connected()
    {
        Assert.False(WasapiPipePumpPadding.ShouldWriteSilence(1000, 0, 0, 200));
    }

    [Fact]
    public void ShouldWriteSilence_false_within_delay_after_connection_without_pcm()
    {
        const long connectedAt = 1000;
        Assert.False(WasapiPipePumpPadding.ShouldWriteSilence(1100, connectedAt, 0, 200));
    }

    [Fact]
    public void ShouldWriteSilence_true_after_delay_since_connection()
    {
        const long connectedAt = 1000;
        Assert.True(WasapiPipePumpPadding.ShouldWriteSilence(1250, connectedAt, 0, 200));
    }

    [Fact]
    public void ShouldWriteSilence_resets_after_real_pcm()
    {
        const long connectedAt = 1000;
        const long lastPcmAt = 1300;
        Assert.False(WasapiPipePumpPadding.ShouldWriteSilence(1400, connectedAt, lastPcmAt, 200));
        Assert.True(WasapiPipePumpPadding.ShouldWriteSilence(1550, connectedAt, lastPcmAt, 200));
    }
}
