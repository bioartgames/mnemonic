using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class ClipIdentityTests
{
    [Fact]
    public void FormatClipId_matches_scratch_naming()
    {
        Assert.Equal("mn_123_ab12_segment_00007", ClipIdentity.FormatClipId("mn_123_ab12", 7));
    }

    [Fact]
    public void TryParseClipId_parses_session_scoped_id()
    {
        Assert.True(ClipIdentity.TryParseClipId("mn_123_ab12_segment_00007", out var prefix, out var index));
        Assert.Equal("mn_123_ab12", prefix);
        Assert.Equal(7, index);
    }

    [Fact]
    public void TryParseClipId_parses_legacy_segment_only_id()
    {
        Assert.True(ClipIdentity.TryParseClipId("segment_00003", out var prefix, out var index));
        Assert.Equal("", prefix);
        Assert.Equal(3, index);
    }
}
