namespace Mnemonic.Events;

public static class SegmentEventResolver
{
    public static IReadOnlyList<SessionEvent> Resolve(
        SessionEventStore store,
        double tOpenUnix,
        double tCloseUnix)
    {
        var window = store.EventsBetween(tOpenUnix, tCloseUnix).ToList();
        var atOrBefore = store.EventsAtOrBefore(tCloseUnix);
        if (PlaytestSpanEvaluator.ShouldEmitOngoing(tOpenUnix, tCloseUnix, window, atOrBefore))
        {
            window.Add(PlaytestSpanEvaluator.CreateOngoingEvent(tCloseUnix));
        }

        return window;
    }
}
