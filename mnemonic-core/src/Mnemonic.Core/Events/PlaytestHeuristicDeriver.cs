namespace Mnemonic.Events;

public sealed class PlaytestHeuristicDeriver
{
    private readonly List<double> _rapidStarts = [];
    private double _lastRapidEmitUnix = double.NegativeInfinity;

    public IEnumerable<SessionEvent> Process(SessionEvent raw)
    {
        if (raw.Type == "playtest_start")
        {
            foreach (var derived in ProcessPlaytestStart(raw.T))
            {
                yield return derived;
            }
        }
        else if (raw.Type == "playtest_stop")
        {
            var derived = ProcessPlaytestStop(raw);
            if (derived is not null)
            {
                yield return derived;
            }
        }
    }

    private IEnumerable<SessionEvent> ProcessPlaytestStart(double nu)
    {
        _rapidStarts.Add(nu);

        while (_rapidStarts.Count > 0 && _rapidStarts[0] < nu - MnemonicConstants.RapidPlaytestWindowSeconds)
        {
            _rapidStarts.RemoveAt(0);
        }

        if (_rapidStarts.Count >= MnemonicConstants.RapidPlaytestMinStarts
            && nu - _lastRapidEmitUnix >= MnemonicConstants.RapidPlaytestWindowSeconds)
        {
            _lastRapidEmitUnix = nu;
            yield return SessionEvent.Create(nu, "rapid_playtest");
        }
    }

    private static SessionEvent? ProcessPlaytestStop(SessionEvent stop)
    {
        var duration = 0.0;
        if (stop.Extra is not null
            && stop.Extra.TryGetValue("duration_sec", out var durEl)
            && durEl.TryGetDouble(out var parsed))
        {
            duration = parsed;
        }

        if (duration > MnemonicConstants.LongPlaytestDurationSeconds)
        {
            return SessionEvent.Create(stop.T, "long_playtest");
        }

        return null;
    }
}
