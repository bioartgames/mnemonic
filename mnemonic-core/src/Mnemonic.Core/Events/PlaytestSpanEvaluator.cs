namespace Mnemonic.Events;

public static class PlaytestSpanEvaluator
{
    private const double OngoingEventEpsilonSeconds = 0.001;

    public static bool IsOpenPlaytestAt(double tCloseUnix, IReadOnlyList<SessionEvent> eventsAtOrBeforeClose)
    {
        SessionEvent? latestStart = null;
        foreach (var e in eventsAtOrBeforeClose)
        {
            if (e.Type != "playtest_start")
            {
                continue;
            }

            if (latestStart is null || e.T > latestStart.T)
            {
                latestStart = e;
            }
        }

        if (latestStart is null)
        {
            return false;
        }

        var startT = latestStart.T;
        foreach (var e in eventsAtOrBeforeClose)
        {
            if (e.Type != "playtest_stop")
            {
                continue;
            }

            if (startT < e.T && e.T <= tCloseUnix)
            {
                return false;
            }
        }

        return true;
    }

    public static bool ShouldEmitOngoing(
        double tOpenUnix,
        double tCloseUnix,
        IReadOnlyList<SessionEvent> windowEvents,
        IReadOnlyList<SessionEvent> eventsAtOrBeforeClose)
    {
        if (!IsOpenPlaytestAt(tCloseUnix, eventsAtOrBeforeClose))
        {
            return false;
        }

        foreach (var e in windowEvents)
        {
            if (e.Type == "playtest_start")
            {
                return false;
            }
        }

        return true;
    }

    public static SessionEvent CreateOngoingEvent(double tCloseUnix) =>
        SessionEvent.Create(tCloseUnix - OngoingEventEpsilonSeconds, "playtest_ongoing");
}
