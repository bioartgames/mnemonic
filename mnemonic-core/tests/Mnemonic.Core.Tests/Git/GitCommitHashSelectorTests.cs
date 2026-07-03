using Mnemonic.Events;
using Mnemonic.Git;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Git;

public sealed class GitCommitHashSelectorTests
{
    [Fact]
    public void Select_last_git_commit_with_commit_wins()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":1,\"type\":\"git_commit\",\"commit\":\"aaaa\",\"subject\":\"first\"}",
                out var first));
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":2,\"type\":\"git_commit\",\"commit\":\"bbbb\",\"subject\":\"second\"}",
                out var second));

        var hash = GitCommitHashSelector.Select([first!, second!], GitSnapshot.Empty);

        Assert.Equal("bbbb", hash);
    }

    [Fact]
    public void Select_falls_back_to_snapshot_when_events_lack_commit()
    {
        var events = new[] { SessionEvent.Create(1, "git_commit") };
        var snapshot = new GitSnapshot("snap123", "main", "subject");

        var hash = GitCommitHashSelector.Select(events, snapshot);

        Assert.Equal("snap123", hash);
    }

    [Fact]
    public void Select_returns_null_when_neither_present()
    {
        var hash = GitCommitHashSelector.Select([], GitSnapshot.Empty);

        Assert.Null(hash);
    }
}
