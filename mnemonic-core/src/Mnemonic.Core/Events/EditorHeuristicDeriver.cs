using Mnemonic;

namespace Mnemonic.Events;

public sealed class EditorHeuristicDeriver
{
    private readonly List<(double T, string Path)> _savesInWindow = [];
    private readonly List<(double T, string Path)> _resourceSavesInWindow = [];
    private double _lastSaveBurstEmitUnix = double.NegativeInfinity;
    private double _lastResourceBurstEmitUnix = double.NegativeInfinity;
    private double _lastSceneSaveUnix;
    private double _lastPlaytestStopUnix;

    public IEnumerable<SessionEvent> ProcessDerived(SessionEvent raw)
    {
        switch (raw.Type)
        {
            case "scene_save":
                foreach (var derived in ProcessSceneSave(raw))
                {
                    yield return derived;
                }

                break;
            case "resource_saved":
                foreach (var derived in ProcessResourceSaved(raw))
                {
                    yield return derived;
                }

                break;
            case "playtest_start":
                foreach (var derived in ProcessPlaytestStart(raw))
                {
                    yield return derived;
                }

                break;
            case "playtest_stop":
                _lastPlaytestStopUnix = raw.T;
                break;
            case "git_commit":
                foreach (var derived in ProcessGitCommit(raw))
                {
                    yield return derived;
                }

                break;
        }
    }

    private IEnumerable<SessionEvent> ProcessSceneSave(SessionEvent raw)
    {
        var path = SessionEventExtras.GetString(raw, "path");
        if (path is null)
        {
            yield break;
        }

        _lastSceneSaveUnix = raw.T;
        _savesInWindow.Add((raw.T, path));
        PruneSaveWindow(raw.T);

        var distinctPaths = _savesInWindow.Select(s => s.Path).Distinct(StringComparer.Ordinal).Count();
        if (distinctPaths >= MnemonicConstants.SaveBurstMinDistinctPaths
            && raw.T - _lastSaveBurstEmitUnix >= MnemonicConstants.SaveBurstCooldownSeconds)
        {
            _lastSaveBurstEmitUnix = raw.T;
            yield return SessionEvent.Create(raw.T, "save_burst");
        }
    }

    private IEnumerable<SessionEvent> ProcessResourceSaved(SessionEvent raw)
    {
        var path = SessionEventExtras.GetString(raw, "path");
        if (path is null)
        {
            yield break;
        }

        _resourceSavesInWindow.Add((raw.T, path));
        PruneResourceWindow(raw.T);

        var distinctPaths = _resourceSavesInWindow.Select(s => s.Path).Distinct(StringComparer.Ordinal).Count();
        if (distinctPaths >= MnemonicConstants.ResourceBurstMinDistinctPaths
            && raw.T - _lastResourceBurstEmitUnix >= MnemonicConstants.ResourceBurstCooldownSeconds)
        {
            _lastResourceBurstEmitUnix = raw.T;
            yield return SessionEvent.Create(raw.T, "resource_burst");
        }
    }

    private IEnumerable<SessionEvent> ProcessPlaytestStart(SessionEvent raw)
    {
        if (_lastSceneSaveUnix > 0
            && raw.T - _lastSceneSaveUnix <= MnemonicConstants.IterationCycleMaxGapSeconds)
        {
            yield return SessionEvent.Create(raw.T, "iteration_cycle");
        }
    }

    private IEnumerable<SessionEvent> ProcessGitCommit(SessionEvent raw)
    {
        if (_lastPlaytestStopUnix > 0
            && raw.T - _lastPlaytestStopUnix <= MnemonicConstants.CommitAfterPlaytestWindowSeconds)
        {
            yield return SessionEvent.Create(raw.T, "commit_after_playtest");
        }
    }

    private void PruneSaveWindow(double now)
    {
        var cutoff = now - MnemonicConstants.SaveBurstWindowSeconds;
        _savesInWindow.RemoveAll(s => s.T < cutoff);
    }

    private void PruneResourceWindow(double now)
    {
        var cutoff = now - MnemonicConstants.ResourceBurstWindowSeconds;
        _resourceSavesInWindow.RemoveAll(s => s.T < cutoff);
    }
}
