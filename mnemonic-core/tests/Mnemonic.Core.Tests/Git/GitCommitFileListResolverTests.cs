using Mnemonic;
using Mnemonic.Git;
using Xunit;

namespace Mnemonic.Core.Tests.Git;

public sealed class GitCommitFileListResolverTests
{
    [Fact]
    public void Resolve_parses_multiple_paths()
    {
        var runner = new FakeGitCommandRunner();
        runner.EnqueueOk("src/a.cs\nsrc/b.cs\n");
        var resolver = new GitCommitFileListResolver(runner, "/repo");

        var paths = resolver.Resolve("deadbeef");

        Assert.Equal(["src/a.cs", "src/b.cs"], paths);
    }

    [Fact]
    public void Resolve_caps_at_max_count()
    {
        var lines = string.Join('\n', Enumerable.Range(0, 60).Select(i => $"file{i}.cs"));
        var runner = new FakeGitCommandRunner();
        runner.EnqueueOk(lines + "\n");
        var resolver = new GitCommitFileListResolver(runner, "/repo");

        var paths = resolver.Resolve("abc");

        Assert.Equal(MnemonicConstants.MaxFilesModifiedCount, paths.Count);
        Assert.Equal("file0.cs", paths[0]);
        Assert.Equal($"file{MnemonicConstants.MaxFilesModifiedCount - 1}.cs", paths[^1]);
    }

    [Fact]
    public void Resolve_skips_path_over_max_length()
    {
        var longPath = new string('x', MnemonicConstants.MaxFilesModifiedPathLength + 1);
        var runner = new FakeGitCommandRunner();
        runner.EnqueueOk($"ok.cs\n{longPath}\n");
        var resolver = new GitCommitFileListResolver(runner, "/repo");

        var paths = resolver.Resolve("abc");

        Assert.Single(paths);
        Assert.Equal("ok.cs", paths[0]);
    }

    [Fact]
    public void Resolve_returns_empty_when_run_fails()
    {
        var runner = new FakeGitCommandRunner();
        runner.EnqueueFail();
        var resolver = new GitCommitFileListResolver(runner, "/repo");

        var paths = resolver.Resolve("abc");

        Assert.Empty(paths);
    }

    [Fact]
    public void Resolve_returns_empty_when_repo_root_null()
    {
        var runner = new FakeGitCommandRunner();
        runner.EnqueueOk("a.cs\n");
        var resolver = new GitCommitFileListResolver(runner, null);

        var paths = resolver.Resolve("abc");

        Assert.Empty(paths);
    }

    [Fact]
    public void Resolve_returns_empty_when_hash_null()
    {
        var runner = new FakeGitCommandRunner();
        runner.EnqueueOk("a.cs\n");
        var resolver = new GitCommitFileListResolver(runner, "/repo");

        var paths = resolver.Resolve(null);

        Assert.Empty(paths);
    }

    private sealed class FakeGitCommandRunner : IGitCommandRunner
    {
        private readonly Queue<GitCommandResult> _results = new();

        public void EnqueueOk(string stdout) => _results.Enqueue(new GitCommandResult(true, 0, stdout));

        public void EnqueueFail() => _results.Enqueue(new GitCommandResult(false, 1, ""));

        public GitCommandResult Run(string repoRoot, IReadOnlyList<string> args) => Dequeue();

        public GitCommandResult RunFullOutput(string repoRoot, IReadOnlyList<string> args) => Dequeue();

        private GitCommandResult Dequeue() =>
            _results.Count > 0 ? _results.Dequeue() : new GitCommandResult(false, 1, "");
    }
}
