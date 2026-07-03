using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class PlaytestSpanEvaluatorTests
{
    [Fact]
    public void IsOpenPlaytestAt_true_when_start_without_stop_before_close()
    {
        var events = new List<SessionEvent>
        {
            SessionEvent.Create(100, "playtest_start"),
        };

        Assert.True(PlaytestSpanEvaluator.IsOpenPlaytestAt(200, events));
    }

    [Fact]
    public void IsOpenPlaytestAt_false_when_stop_before_close()
    {
        var events = new List<SessionEvent>
        {
            SessionEvent.Create(100, "playtest_start"),
            SessionEvent.Create(150, "playtest_stop"),
        };

        Assert.False(PlaytestSpanEvaluator.IsOpenPlaytestAt(200, events));
    }

    [Fact]
    public void ShouldEmitOngoing_false_when_start_in_window()
    {
        var window = new List<SessionEvent> { SessionEvent.Create(105, "playtest_start") };
        var atOrBefore = new List<SessionEvent>
        {
            SessionEvent.Create(105, "playtest_start"),
        };

        Assert.False(PlaytestSpanEvaluator.ShouldEmitOngoing(100, 120, window, atOrBefore));
    }

    [Fact]
    public void ShouldEmitOngoing_true_when_start_before_window_only()
    {
        var window = new List<SessionEvent>();
        var atOrBefore = new List<SessionEvent>
        {
            SessionEvent.Create(90, "playtest_start"),
        };

        Assert.True(PlaytestSpanEvaluator.ShouldEmitOngoing(100, 120, window, atOrBefore));
    }

    [Fact]
    public void CreateOngoingEvent_falls_inside_window()
    {
        var evt = PlaytestSpanEvaluator.CreateOngoingEvent(120);
        Assert.Equal("playtest_ongoing", evt.Type);
        Assert.True(evt.T >= 100 && evt.T < 120);
    }
}
