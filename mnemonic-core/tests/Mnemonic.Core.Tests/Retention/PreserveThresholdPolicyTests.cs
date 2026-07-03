using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class PreserveThresholdPolicyTests
{
    [Theory]
    [InlineData(10, 10)]
    [InlineData(0, 1)]
    [InlineData(999, 500)]
    public void Clamp_clamps_to_valid_range(int input, int expected)
    {
        Assert.Equal(expected, PreserveThresholdPolicy.Clamp(input));
    }
}
