using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class ActivityPacketWindowPolicyTests
{
    [Theory]
    [InlineData(30, 15)]
    [InlineData(60, 30)]
    [InlineData(120, 60)]
    [InlineData(240, 120)]
    [InlineData(600, 120)]
    public void Compute_clamps_half_segment(int segmentSeconds, int expectedWindow)
    {
        Assert.Equal(expectedWindow, ActivityPacketWindowPolicy.Compute(segmentSeconds));
    }
}
