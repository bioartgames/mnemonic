using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class SegmentHistoryMaxEntriesPolicyTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(9, 10)]
    [InlineData(10, 10)]
    [InlineData(200, 200)]
    [InlineData(1000, 1000)]
    [InlineData(5000, 1000)]
    public void Clamp_bounds(int input, int expected) =>
        Assert.Equal(expected, SegmentHistoryMaxEntriesPolicy.Clamp(input));
}
