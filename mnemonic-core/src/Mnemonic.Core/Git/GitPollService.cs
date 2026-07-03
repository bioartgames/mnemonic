using Mnemonic.Events;
using Mnemonic.Ipc;
using Mnemonic.Retention;

namespace Mnemonic.Git;

public sealed class GitPollService
{
    private readonly DataRootPaths _paths;
    private readonly IGitCommandRunner _runner;
    private readonly EventIngestService _ingest;
    private readonly string? _repoRoot;
    private bool _probeDisabled;
    private string _lastHead = "";
    private string _lastBranch = "";
    private string _lastSubject = "";
    private int _lastAhead;

    public GitPollService(
        DataRootPaths paths,
        IGitCommandRunner runner,
        EventIngestService ingest,
        string? repoRoot = null)
    {
        _paths = paths;
        _runner = runner;
        _ingest = ingest;
        _repoRoot = repoRoot ?? GitRepositoryLocator.FindRepoRoot();
        if (_repoRoot is null)
        {
            _probeDisabled = true;
        }
    }

    public bool ProbeDisabled => _probeDisabled;

    public IGitCommandRunner CommandRunner => _runner;

    public string? RepositoryRoot => _repoRoot;

    public GitSnapshot CurrentSnapshot => new(_lastHead, _lastBranch, _lastSubject);

    public void InitializeBaseline()
    {
        if (_probeDisabled || _repoRoot is null)
        {
            return;
        }

        if (!TryReadSnapshot(out var head, out var branch, out var subject))
        {
            return;
        }

        _lastHead = head;
        _lastBranch = branch;
        _lastSubject = subject;
        _lastAhead = GitAheadCountResolver.GetAheadCount(_runner, _repoRoot);
    }

    public void Tick()
    {
        if (_probeDisabled || _repoRoot is null)
        {
            return;
        }

        var headResult = _runner.Run(_repoRoot, ["rev-parse", "HEAD"]);
        if (!headResult.Ok)
        {
            DisableProbe();
            return;
        }

        var head = headResult.Stdout.Trim();
        if (head.Length == 0)
        {
            DisableProbe();
            return;
        }

        var branch = "";
        var branchResult = _runner.Run(_repoRoot, ["rev-parse", "--abbrev-ref", "HEAD"]);
        if (branchResult.Ok)
        {
            branch = branchResult.Stdout.Trim();
        }

        var oldHead = _lastHead;
        var oldBranch = _lastBranch;

        if (head != oldHead)
        {
            var subject = "";
            var subjectResult = _runner.Run(_repoRoot, ["log", "-1", "--pretty=%s"]);
            if (subjectResult.Ok)
            {
                subject = subjectResult.Stdout.Trim();
            }

            _lastSubject = subject;
            if (!JsonlGitEventDeduper.HasRecentGitCommit(_paths.SessionEventsFile, head))
            {
                var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                JsonlEventAppender.Append(
                    _paths.SessionEventsFile,
                    SessionEventJson.CreateGitCommit(t, head, subject));
            }
        }

        if (branch != oldBranch && branch.Length > 0)
        {
            if (!JsonlGitEventDeduper.HasRecentGitBranchChange(_paths.SessionEventsFile, branch))
            {
                var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                JsonlEventAppender.Append(
                    _paths.SessionEventsFile,
                    SessionEventJson.CreateGitBranchChange(t, branch));
            }
        }

        var ahead = GitAheadCountResolver.GetAheadCount(_runner, _repoRoot);
        if (_lastAhead > 0 && ahead == 0 && head == oldHead && branch.Length > 0
            && !JsonlGitEventDeduper.HasRecentGitPush(
                _paths.SessionEventsFile,
                branch,
                MnemonicConstants.GitPushDedupeWindowSeconds))
        {
            var remote = "";
            if (GitAheadCountResolver.TryParseUpstream(_runner, _repoRoot, out var parsedRemote, out _))
            {
                remote = parsedRemote;
            }

            var tPush = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            JsonlEventAppender.Append(
                _paths.SessionEventsFile,
                SessionEventJson.CreateGitPush(tPush, branch, remote));
        }

        _lastAhead = ahead;
        _lastHead = head;
        _lastBranch = branch;

        _ingest.Poll();
    }

    private bool TryReadSnapshot(out string head, out string branch, out string subject)
    {
        head = "";
        branch = "";
        subject = "";

        var headResult = _runner.Run(_repoRoot!, ["rev-parse", "HEAD"]);
        if (!headResult.Ok)
        {
            return false;
        }

        head = headResult.Stdout.Trim();
        if (head.Length == 0)
        {
            return false;
        }

        var branchResult = _runner.Run(_repoRoot!, ["rev-parse", "--abbrev-ref", "HEAD"]);
        if (branchResult.Ok)
        {
            branch = branchResult.Stdout.Trim();
        }

        var subjectResult = _runner.Run(_repoRoot!, ["log", "-1", "--pretty=%s"]);
        if (subjectResult.Ok)
        {
            subject = subjectResult.Stdout.Trim();
        }

        return true;
    }

    private void DisableProbe()
    {
        _probeDisabled = true;
    }
}
