using Mnemonic.Events;
using Mnemonic.Heuristic;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Heuristic;

public sealed class HeuristicScorerTests
{
    [Theory]
    [InlineData("playtest_start", 7)]
    [InlineData("playtest_stop", 0)]
    [InlineData("rapid_playtest", 9)]
    [InlineData("long_playtest", 8)]
    [InlineData("runtime_error", 9)]
    [InlineData("scene_save", 5)]
    [InlineData("scene_transition", 4)]
    [InlineData("git_commit", 9)]
    [InlineData("git_branch_change", 6)]
    [InlineData("save_burst", 6)]
    [InlineData("iteration_cycle", 10)]
    [InlineData("commit_after_playtest", 10)]
    [InlineData("unknown_type", 0)]
    public void Score_maps_type_weights(string type, int expectedWeight)
    {
        var score = HeuristicScorer.Score([SessionEvent.Create(1, type)]);
        Assert.Equal(expectedWeight, score);
    }

    [Fact]
    public void Score_empty_list_returns_zero()
    {
        Assert.Equal(0, HeuristicScorer.Score([]));
    }

    [Fact]
    public void Score_sums_multiple_events()
    {
        var events = new[]
        {
            SessionEvent.Create(1, "scene_save"),
            SessionEvent.Create(2, "git_commit"),
        };
        Assert.Equal(14, HeuristicScorer.Score(events));
    }

    [Fact]
    public void Score_disabled_type_contributes_zero()
    {
        var settings = new AppSettings
        {
            Heuristics = new Dictionary<string, HeuristicTypeSettings>
            {
                ["git_commit"] = new() { Enabled = false, Weight = 9 },
            },
        };
        var score = HeuristicScorer.Score([SessionEvent.Create(1, "git_commit")], settings);
        Assert.Equal(0, score);
    }

    [Fact]
    public void Score_custom_weight_overrides_default()
    {
        var settings = new AppSettings
        {
            Heuristics = new Dictionary<string, HeuristicTypeSettings>
            {
                ["git_commit"] = new() { Enabled = true, Weight = 3 },
            },
        };
        var score = HeuristicScorer.Score([SessionEvent.Create(1, "git_commit")], settings);
        Assert.Equal(3, score);
    }

    [Fact]
    public void ScoreBreakdown_lines_sum_to_total()
    {
        var events = new[]
        {
            SessionEvent.Create(1, "scene_save"),
            SessionEvent.Create(2, "git_commit"),
        };
        var (total, lines) = HeuristicScorer.ScoreBreakdown(events, settings: null);
        Assert.Equal(14, total);
        Assert.Equal(14, lines.Sum(l => l.Points));
        Assert.Contains(lines, l => l.Type == "git_commit" && l.Points == 9);
    }

    [Fact]
    public void ScoreBreakdown_captures_pattern_detail()
    {
        var events = new[]
        {
            SessionEvent.Create(
                1,
                "edit_intensity",
                new Dictionary<string, object?> { ["pattern_detail"] = "2 scene, 2 resource, 0 transitions, 0s playtest in 60s" }),
        };
        var (_, lines) = HeuristicScorer.ScoreBreakdown(events, settings: null);
        var line = Assert.Single(lines);
        Assert.Equal("edit_intensity", line.Type);
        Assert.Equal("2 scene, 2 resource, 0 transitions, 0s playtest in 60s", line.Detail);
    }
}
