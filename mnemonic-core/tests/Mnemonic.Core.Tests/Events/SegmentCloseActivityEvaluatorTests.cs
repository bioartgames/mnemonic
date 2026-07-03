using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class SegmentCloseActivityEvaluatorTests
{
    [Fact]
    public void Appends_long_edit_span_when_criteria_met()
    {
        var events = new List<SessionEvent>
        {
            SessionEvent.Create(100, "scene_save"),
            SessionEvent.Create(110, "resource_saved"),
            SessionEvent.Create(120, "scene_transition"),
            SessionEvent.Create(130, "scene_save"),
            SessionEvent.Create(140, "resource_saved"),
        };

        SegmentCloseActivityEvaluator.AppendSynthetic(events, 100, 200);
        Assert.Single(events, e => e.Type == "long_edit_span" && e.T == 200);
    }

    [Fact]
    public void Skips_when_playtest_present()
    {
        var events = new List<SessionEvent>
        {
            SessionEvent.Create(100, "playtest_start"),
            SessionEvent.Create(110, "scene_save"),
            SessionEvent.Create(120, "scene_save"),
            SessionEvent.Create(130, "scene_save"),
            SessionEvent.Create(140, "scene_save"),
            SessionEvent.Create(150, "scene_save"),
        };

        SegmentCloseActivityEvaluator.AppendSynthetic(events, 100, 200);
        Assert.DoesNotContain(events, e => e.Type == "long_edit_span");
    }
}
