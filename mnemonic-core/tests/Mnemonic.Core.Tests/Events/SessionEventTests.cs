using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class SessionEventTests
{
    [Fact]
    public void TryParseFromJsonLine_parses_valid_line()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine("{\"t\":150,\"type\":\"scene_save\",\"path\":\"x.tscn\"}", out var evt));
        Assert.NotNull(evt);
        Assert.Equal(150, evt!.T);
        Assert.Equal("scene_save", evt.Type);
        Assert.NotNull(evt.Extra);
        Assert.True(evt.Extra!.ContainsKey("path"));
    }

    [Fact]
    public void TryParseFromJsonLine_rejects_missing_type()
    {
        Assert.False(SessionEvent.TryParseFromJsonLine("{\"t\":150}", out _));
    }
}
