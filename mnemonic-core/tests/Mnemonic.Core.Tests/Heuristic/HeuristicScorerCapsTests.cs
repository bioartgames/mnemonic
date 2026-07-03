using Mnemonic.Events;
using Mnemonic.Heuristic;
using Xunit;

namespace Mnemonic.Core.Tests.Heuristic;

public sealed class HeuristicScorerCapsTests
{
    [Fact]
    public void Ten_scene_saves_capped_at_three()
    {
        var events = Enumerable.Range(0, 10)
            .Select(i => SessionEvent.Create(100 + i, "scene_save"))
            .ToList();
        Assert.Equal(15, HeuristicScorer.Score(events));
    }

    [Fact]
    public void Three_transitions_capped_at_two()
    {
        var events = Enumerable.Range(0, 3)
            .Select(i => SessionEvent.Create(100 + i, "scene_transition"))
            .ToList();
        Assert.Equal(8, HeuristicScorer.Score(events));
    }

    [Fact]
    public void Six_playtest_starts_capped_at_five()
    {
        var events = Enumerable.Range(0, 6)
            .Select(i => SessionEvent.Create(100 + i, "playtest_start"))
            .ToList();
        Assert.Equal(35, HeuristicScorer.Score(events));
    }

    [Fact]
    public void Two_git_commits_capped_at_one()
    {
        var events = new[]
        {
            SessionEvent.Create(100, "git_commit"),
            SessionEvent.Create(110, "git_commit"),
        };
        Assert.Equal(9, HeuristicScorer.Score(events));
    }

    [Fact]
    public void Six_runtime_errors_capped_at_three()
    {
        var events = Enumerable.Range(0, 6)
            .Select(i => SessionEvent.Create(100 + i, "runtime_error"))
            .ToList();
        Assert.Equal(27, HeuristicScorer.Score(events));
    }

    [Fact]
    public void Derived_events_not_capped()
    {
        var events = Enumerable.Range(0, 3)
            .Select(i => SessionEvent.Create(1000 + i, "rapid_playtest"))
            .ToList();
        Assert.Equal(27, HeuristicScorer.Score(events));
    }
}
