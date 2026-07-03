using Mnemonic.Events;
using Mnemonic.Git;
using Xunit;

namespace Mnemonic.Core.Tests.Git;

public sealed class JsonlGitEventDeduperTests
{
    [Fact]
    public void HasRecentGitPush_finds_matching_branch_in_window()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mnemonic_dedupe_{Guid.NewGuid():N}.jsonl");
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var evt = SessionEventJson.CreateGitPush(now, "main", "origin");
            File.WriteAllText(path, SessionEventJson.ToJsonLine(evt) + "\n");

            Assert.True(JsonlGitEventDeduper.HasRecentGitPush(path, "main", 120));
            Assert.False(JsonlGitEventDeduper.HasRecentGitPush(path, "feature", 120));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void HasRecentGitCommit_finds_matching_commit_in_tail()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mnemonic_dedupe_{Guid.NewGuid():N}.jsonl");
        try
        {
            var evt = SessionEventJson.CreateGitCommit(1, "deadbeef", "subject");
            File.WriteAllText(path, SessionEventJson.ToJsonLine(evt) + "\n");

            Assert.True(JsonlGitEventDeduper.HasRecentGitCommit(path, "deadbeef"));
            Assert.False(JsonlGitEventDeduper.HasRecentGitCommit(path, "other"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
