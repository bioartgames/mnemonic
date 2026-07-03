namespace Mnemonic.Events;

public sealed class SessionEventStore
{
    private readonly List<SessionEvent> _entries = [];

    public int Count => _entries.Count;

    public void Append(SessionEvent evt)
    {
        _entries.Add(evt);
        TrimIfNeeded();
    }

    public IReadOnlyList<SessionEvent> EventsBetween(double tOpenUnix, double tCloseUnix)
    {
        var outList = new List<SessionEvent>();
        foreach (var e in _entries)
        {
            if (tOpenUnix <= e.T && e.T < tCloseUnix)
            {
                outList.Add(e);
            }
        }

        return outList;
    }

    public IReadOnlyList<SessionEvent> EventsAtOrBefore(double tCloseUnix)
    {
        var outList = new List<SessionEvent>();
        foreach (var e in _entries)
        {
            if (e.T <= tCloseUnix)
            {
                outList.Add(e);
            }
        }

        return outList;
    }

    private void TrimIfNeeded()
    {
        var excess = _entries.Count - MnemonicConstants.SessionEventsMaxEntries;
        if (excess <= 0)
        {
            return;
        }

        _entries.RemoveRange(0, excess);
    }
}
