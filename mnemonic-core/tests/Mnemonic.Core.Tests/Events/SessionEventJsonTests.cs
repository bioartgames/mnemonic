using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class SessionEventJsonTests
{
    [Fact]
    public void Git_commit_round_trips_through_json_line()
    {
        var evt = SessionEventJson.CreateGitCommit(150, "abc123", "Fix capture");
        var line = SessionEventJson.ToJsonLine(evt);

        Assert.True(SessionEvent.TryParseFromJsonLine(line, out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal("git_commit", parsed!.Type);
        Assert.Equal(150, parsed.T);
        Assert.Equal("abc123", parsed.Extra!["commit"].GetString());
        Assert.Equal("Fix capture", parsed.Extra["subject"].GetString());
    }
}
