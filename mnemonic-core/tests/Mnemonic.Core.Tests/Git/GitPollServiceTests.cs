using Mnemonic.Events;
using Mnemonic.Git;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Git;

public sealed class GitPollServiceTests
{
    [Fact]
    public void InitializeBaseline_does_not_append()
    {
        var root = CreateTempDataRoot();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(repo);
        try
        {
            var runner = new FakeGitCommandRunner();
            runner.EnqueueOk("aaaa");
            runner.EnqueueOk("main");
            runner.EnqueueOk("initial");

            var ingest = new EventIngestService(new DataRootPaths(root), SettingsDefaults.Create());
            var poll = new GitPollService(new DataRootPaths(root), runner, ingest, repo);
            poll.InitializeBaseline();

            Assert.False(File.Exists(Path.Combine(root, "events", "session_events.jsonl")));
            Assert.Equal(new GitSnapshot("aaaa", "main", "initial"), poll.CurrentSnapshot);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Tick_head_change_appends_git_commit()
    {
        var root = CreateTempDataRoot();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(repo);
        try
        {
            var runner = new FakeGitCommandRunner();
            runner.EnqueueOk("aaaa");
            runner.EnqueueOk("main");
            runner.EnqueueOk("initial");

            var paths = new DataRootPaths(root);
            var ingest = new EventIngestService(paths, SettingsDefaults.Create());
            var poll = new GitPollService(paths, runner, ingest, repo);
            poll.InitializeBaseline();

            runner.EnqueueOk("bbbb");
            runner.EnqueueOk("main");
            runner.EnqueueOk("second commit");

            poll.Tick();

            var jsonl = File.ReadAllText(paths.SessionEventsFile);
            Assert.Single(jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries));
            Assert.Contains("bbbb", jsonl);
            Assert.Contains("second commit", jsonl);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Assert.Equal(9, ingest.ScoreWindow(now - 60, now + 60));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Tick_same_head_no_append()
    {
        var root = CreateTempDataRoot();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(repo);
        try
        {
            var runner = new FakeGitCommandRunner();
            runner.EnqueueOk("aaaa");
            runner.EnqueueOk("main");
            runner.EnqueueOk("initial");

            var paths = new DataRootPaths(root);
            var ingest = new EventIngestService(paths, SettingsDefaults.Create());
            var poll = new GitPollService(paths, runner, ingest, repo);
            poll.InitializeBaseline();

            runner.EnqueueOk("bbbb");
            runner.EnqueueOk("main");
            runner.EnqueueOk("second");
            poll.Tick();

            runner.EnqueueOk("bbbb");
            runner.EnqueueOk("main");
            poll.Tick();

            var lines = File.ReadAllLines(paths.SessionEventsFile);
            Assert.Single(lines);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Tick_branch_change_appends()
    {
        var root = CreateTempDataRoot();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(repo);
        try
        {
            var runner = new FakeGitCommandRunner();
            runner.EnqueueOk("aaaa");
            runner.EnqueueOk("main");
            runner.EnqueueOk("initial");

            var paths = new DataRootPaths(root);
            var ingest = new EventIngestService(paths, SettingsDefaults.Create());
            var poll = new GitPollService(paths, runner, ingest, repo);
            poll.InitializeBaseline();

            runner.EnqueueOk("aaaa");
            runner.EnqueueOk("feature");
            poll.Tick();

            var jsonl = File.ReadAllText(paths.SessionEventsFile);
            Assert.Contains("git_branch_change", jsonl);
            Assert.Contains("feature", jsonl);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Assert.Equal(6, ingest.ScoreWindow(now - 60, now + 60));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Tick_dedupes_existing_hook_commit()
    {
        var root = CreateTempDataRoot();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(repo);
        try
        {
            var paths = new DataRootPaths(root);
            var existing = SessionEventJson.CreateGitCommit(100, "bbbb", "from hook");
            File.WriteAllText(paths.SessionEventsFile, SessionEventJson.ToJsonLine(existing) + "\n");

            var runner = new FakeGitCommandRunner();
            runner.EnqueueOk("aaaa");
            runner.EnqueueOk("main");
            runner.EnqueueOk("initial");

            var ingest = new EventIngestService(paths, SettingsDefaults.Create());
            var poll = new GitPollService(paths, runner, ingest, repo);
            poll.InitializeBaseline();

            runner.EnqueueOk("bbbb");
            runner.EnqueueOk("main");
            runner.EnqueueOk("polled subject");
            poll.Tick();

            var lines = File.ReadAllLines(paths.SessionEventsFile);
            Assert.Single(lines);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Tick_push_emits_once_when_ahead_drops()
    {
        var root = CreateTempDataRoot();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(repo);
        try
        {
            var runner = new ArgAwareGitRunner();
            runner.EnqueueBaseline("aaaa", "main", "initial", ahead: 2);
            runner.EnqueueTick("aaaa", "main", ahead: 0);

            var paths = new DataRootPaths(root);
            var ingest = new EventIngestService(paths, SettingsDefaults.Create());
            var poll = new GitPollService(paths, runner, ingest, repo);
            poll.InitializeBaseline();
            poll.Tick();

            var jsonl = File.ReadAllText(paths.SessionEventsFile);
            Assert.Contains("git_push", jsonl);
            Assert.Contains("main", jsonl);
            Assert.Single(jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries));

            runner.EnqueueTick("aaaa", "main", ahead: 0);
            poll.Tick();
            var lines = File.ReadAllLines(paths.SessionEventsFile);
            Assert.Single(lines);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Tick_probe_failure_disables()
    {
        var root = CreateTempDataRoot();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(repo);
        try
        {
            var runner = new FakeGitCommandRunner();
            runner.EnqueueFail();

            var paths = new DataRootPaths(root);
            var ingest = new EventIngestService(paths, SettingsDefaults.Create());
            var poll = new GitPollService(paths, runner, ingest, repo);
            poll.Tick();

            Assert.True(poll.ProbeDisabled);
            poll.Tick();
            Assert.False(File.Exists(paths.SessionEventsFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDataRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_gitpoll_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "events"));
        return root;
    }

    private sealed class FakeGitCommandRunner : IGitCommandRunner
    {
        private readonly Queue<GitCommandResult> _results = new();

        public void EnqueueOk(string stdout) => _results.Enqueue(new GitCommandResult(true, 0, stdout));

        public void EnqueueFail() => _results.Enqueue(new GitCommandResult(false, 1, ""));

        public GitCommandResult Run(string repoRoot, IReadOnlyList<string> args) =>
            Dequeue();

        public GitCommandResult RunFullOutput(string repoRoot, IReadOnlyList<string> args) =>
            Dequeue();

        private GitCommandResult Dequeue() =>
            _results.Count > 0 ? _results.Dequeue() : new GitCommandResult(false, 1, "");
    }

    private sealed class ArgAwareGitRunner : IGitCommandRunner
    {
        private readonly Queue<Func<IReadOnlyList<string>, GitCommandResult>> _handlers = new();

        public void EnqueueBaseline(string head, string branch, string subject, int ahead)
        {
            EnqueueHead(head);
            EnqueueBranch(branch);
            EnqueueSubject(subject);
            EnqueueUpstream();
            EnqueueAhead(ahead);
        }

        public void EnqueueTick(string head, string branch, int ahead)
        {
            EnqueueHead(head);
            EnqueueBranch(branch);
            EnqueueUpstream();
            EnqueueAhead(ahead);
            EnqueueUpstream();
        }

        private void EnqueueHead(string head) =>
            _handlers.Enqueue(args =>
                args.Contains("HEAD")
                    ? Ok(head)
                    : Fail());

        private void EnqueueBranch(string branch) =>
            _handlers.Enqueue(args =>
                args.Contains("--abbrev-ref")
                    ? Ok(branch)
                    : Fail());

        private void EnqueueSubject(string subject) =>
            _handlers.Enqueue(args =>
                args.Contains("--pretty=%s")
                    ? Ok(subject)
                    : Fail());

        private void EnqueueUpstream() =>
            _handlers.Enqueue(args =>
                args.Contains("@{u}") && !args.Contains("rev-list")
                    ? Ok("origin/main")
                    : Fail());

        private void EnqueueAhead(int ahead) =>
            _handlers.Enqueue(args =>
                args.Contains("rev-list")
                    ? Ok(ahead.ToString())
                    : Fail());

        public GitCommandResult Run(string repoRoot, IReadOnlyList<string> args) =>
            _handlers.Count > 0 ? _handlers.Dequeue()(args) : Fail();

        public GitCommandResult RunFullOutput(string repoRoot, IReadOnlyList<string> args) => Run(repoRoot, args);

        private static GitCommandResult Ok(string stdout) => new(true, 0, stdout);

        private static GitCommandResult Fail() => new(false, 1, "");
    }
}
