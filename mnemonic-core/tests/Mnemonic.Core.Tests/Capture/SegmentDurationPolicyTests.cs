using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class SegmentDurationPolicyTests
{
    [Theory]
    [InlineData(29, 30)]
    [InlineData(601, 600)]
    [InlineData(0, 120)]
    [InlineData(90, 90)]
    public void Normalize_clamps_and_defaults(int raw, int expected)
    {
        Assert.Equal(expected, SegmentDurationPolicy.Normalize(raw));
    }
}
