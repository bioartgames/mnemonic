using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class HighlightScoreMinPolicyTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(25, 25)]
    [InlineData(600, 500)]
    public void Clamp_bounds(int input, int expected)
    {
        Assert.Equal(expected, HighlightScoreMinPolicy.Clamp(input));
    }
}
