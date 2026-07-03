using Mnemonic;
using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class ScratchCapPolicyTests
{
    [Theory]
    [InlineData(8, 8)]
    [InlineData(0, 1)]
    [InlineData(999, 128)]
    public void ClampGb_clamps_to_valid_range(int input, int expected)
    {
        Assert.Equal(expected, ScratchCapPolicy.ClampGb(input));
    }

    [Fact]
    public void ToCapBytes_default_8gb()
    {
        Assert.Equal(8L * MnemonicConstants.ScratchCapBytesPerGb, ScratchCapPolicy.ToCapBytes(8));
    }

    [Fact]
    public void ToCapBytes_1gb_floors_to_min()
    {
        Assert.Equal(MnemonicConstants.ScratchCapMinBytes, ScratchCapPolicy.ToCapBytes(1));
    }
}
