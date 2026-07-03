using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class PlaytestHeuristicDeriverTests
{
    [Fact]
    public void Three_starts_within_window_emits_rapid_playtest_once()
    {
        var deriver = new PlaytestHeuristicDeriver();
        var all = new List<SessionEvent>();

        foreach (var t in new[] { 1000.0, 1100.0, 1200.0 })
        {
            all.AddRange(deriver.Process(SessionEvent.Create(t, "playtest_start")));
        }

        Assert.Single(all, e => e.Type == "rapid_playtest" && e.T == 1200);
    }

    [Fact]
    public void Fourth_start_within_cooldown_does_not_emit_second_rapid()
    {
        var deriver = new PlaytestHeuristicDeriver();
        var all = new List<SessionEvent>();
        foreach (var t in new[] { 1000.0, 1100.0, 1200.0, 1250.0 })
        {
            all.AddRange(deriver.Process(SessionEvent.Create(t, "playtest_start")));
        }

        Assert.Single(all, e => e.Type == "rapid_playtest");
    }

    [Fact]
    public void Playtest_stop_over_duration_emits_long_playtest()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":2000,\"type\":\"playtest_stop\",\"duration_sec\":200}",
                out var stop));
        var deriver = new PlaytestHeuristicDeriver();
        var batch = deriver.Process(stop!).ToList();
        Assert.DoesNotContain(batch, e => e.Type == "playtest_stop");
        Assert.Contains(batch, e => e.Type == "long_playtest" && e.T == 2000);
    }

    [Fact]
    public void Playtest_stop_at_duration_threshold_does_not_emit_long()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":2000,\"type\":\"playtest_stop\",\"duration_sec\":180}",
                out var stop));
        var deriver = new PlaytestHeuristicDeriver();
        var batch = deriver.Process(stop!).ToList();
        Assert.DoesNotContain(batch, e => e.Type == "playtest_stop");
        Assert.DoesNotContain(batch, e => e.Type == "long_playtest");
    }
}
