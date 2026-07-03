using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class SegmentEventResolverTests
{
    [Fact]
    public void Resolve_appends_playtest_ongoing_when_span_open_and_no_start_in_window()
    {
        var store = new SessionEventStore();
        store.Append(SessionEvent.Create(90, "playtest_start"));

        var resolved = SegmentEventResolver.Resolve(store, 100, 120);

        Assert.Equal(1, resolved.Count(e => e.Type == "playtest_ongoing"));
    }

    [Fact]
    public void Resolve_does_not_append_when_start_in_window()
    {
        var store = new SessionEventStore();
        store.Append(SessionEvent.Create(105, "playtest_start"));

        var resolved = SegmentEventResolver.Resolve(store, 100, 120);

        Assert.DoesNotContain(resolved, e => e.Type == "playtest_ongoing");
    }
}
