using Mnemonic.Git;
using Xunit;

namespace Mnemonic.Core.Tests.Git;

public sealed class GitAheadCountResolverTests
{
    [Fact]
    public void GetAheadCount_no_upstream_returns_zero()
    {
        var runner = new FakeGitCommandRunner();
        runner.EnqueueFail();

        Assert.Equal(0, GitAheadCountResolver.GetAheadCount(runner, "/repo"));
    }

    [Fact]
    public void GetAheadCount_returns_parsed_ahead()
    {
        var runner = new FakeGitCommandRunner();
        runner.EnqueueOk("origin/main");
        runner.EnqueueOk("2");

        Assert.Equal(2, GitAheadCountResolver.GetAheadCount(runner, "/repo"));
    }

    [Fact]
    public void GetAheadCount_zero_when_rev_list_empty()
    {
        var runner = new FakeGitCommandRunner();
        runner.EnqueueOk("origin/main");
        runner.EnqueueOk("0");

        Assert.Equal(0, GitAheadCountResolver.GetAheadCount(runner, "/repo"));
    }

    [Fact]
    public void TryParseUpstream_splits_remote_and_branch()
    {
        var runner = new FakeGitCommandRunner();
        runner.EnqueueOk("origin/main");
        runner.EnqueueOk("origin/main");

        var ok = GitAheadCountResolver.TryParseUpstream(runner, "/repo", out var remote, out var branch);

        Assert.True(ok);
        Assert.Equal("origin", remote);
        Assert.Equal("main", branch);
    }

    private sealed class FakeGitCommandRunner : IGitCommandRunner
    {
        private readonly Queue<GitCommandResult> _results = new();

        public void EnqueueOk(string stdout) => _results.Enqueue(new GitCommandResult(true, 0, stdout));

        public void EnqueueFail() => _results.Enqueue(new GitCommandResult(false, 1, ""));

        public GitCommandResult Run(string repoRoot, IReadOnlyList<string> args) =>
            _results.Count > 0 ? _results.Dequeue() : new GitCommandResult(false, 1, "");

        public GitCommandResult RunFullOutput(string repoRoot, IReadOnlyList<string> args) => Run(repoRoot, args);
    }
}
