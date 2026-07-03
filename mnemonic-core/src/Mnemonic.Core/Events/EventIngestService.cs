using Mnemonic.Capture;
using Mnemonic.Heuristic;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;

namespace Mnemonic.Events;

public sealed class EventIngestService
{
    private readonly DataRootPaths _paths;
    private readonly AppSettings _settings;
    private readonly SettingsStore? _settingsStore;
    private readonly object _gate = new();
    private readonly SessionEventStore _store = new();
    private readonly JsonlEventTailer _tailer = new();
    private readonly PlaytestHeuristicDeriver _playtestDeriver = new();
    private readonly EditorHeuristicDeriver _editorDeriver = new();
    private readonly ActivityPacketAggregator _activityPacketAggregator = new();
    private readonly ActivityHeuristicDeriver _activityDeriver = new();

    public EventIngestService(DataRootPaths paths, AppSettings settings, SettingsStore? settingsStore = null)
    {
        _paths = paths;
        _settings = settings;
        _settingsStore = settingsStore;
    }

    public DataRootPaths Paths => _paths;

    public int LastSegmentIndex { get; private set; } = -1;

    public int LastSegmentScore { get; private set; }

    public void Poll()
    {
        _settingsStore?.TryMergeHookOwnedFieldsFromDisk(_settings);
        _activityPacketAggregator.SetWindowSeconds(
            ActivityPacketWindowPolicy.Compute(_settings.SegmentDurationSeconds));
        lock (_gate)
        {
            IngestNewEvents(_tailer.Poll(_paths.SessionEventsFile));
        }
    }

    public IReadOnlyList<SessionEvent> EventsBetween(double tOpenUnix, double tCloseUnix)
    {
        lock (_gate)
        {
            return _store.EventsBetween(tOpenUnix, tCloseUnix);
        }
    }

    public IReadOnlyList<SessionEvent> EventsForSegment(double tOpenUnix, double tCloseUnix)
    {
        lock (_gate)
        {
            return SegmentEventResolver.Resolve(_store, tOpenUnix, tCloseUnix);
        }
    }

    public IReadOnlyList<SessionEvent> GetScoringEvents(double tOpenUnix, double tCloseUnix)
    {
        var events = EventsForSegment(tOpenUnix, tCloseUnix).ToList();
        SegmentCloseActivityEvaluator.AppendSynthetic(events, tOpenUnix, tCloseUnix);
        return events;
    }

    public int ScoreWindow(double tOpenUnix, double tCloseUnix) =>
        HeuristicScorer.Score(GetScoringEvents(tOpenUnix, tCloseUnix), _settings);

    public (int Total, IReadOnlyList<HeuristicScoreLine> Lines) ScoreBreakdownWindow(
        double tOpenUnix,
        double tCloseUnix) =>
        HeuristicScorer.ScoreBreakdown(GetScoringEvents(tOpenUnix, tCloseUnix), _settings);

    public void OnSegmentClosed(SegmentClosedEventArgs args)
    {
        Poll();
        lock (_gate)
        {
            foreach (var packet in _activityPacketAggregator.Flush(args.TCloseUnix))
            {
                AppendPacketAndDerived(packet);
            }
        }

        var score = ScoreWindow(args.TOpenUnix, args.TCloseUnix);
        lock (_gate)
        {
            LastSegmentIndex = args.Index;
            LastSegmentScore = score;
        }
    }

    private void IngestNewEvents(IReadOnlyList<SessionEvent> rawEvents)
    {
        foreach (var raw in rawEvents)
        {
            _store.Append(raw);

            foreach (var derived in _playtestDeriver.Process(raw))
            {
                _store.Append(derived);
            }

            foreach (var derived in _editorDeriver.ProcessDerived(raw))
            {
                _store.Append(derived);
            }

            foreach (var packet in _activityPacketAggregator.OnRaw(raw))
            {
                AppendPacketAndDerived(packet);
            }

            foreach (var derived in _activityDeriver.Process(raw))
            {
                _store.Append(derived);
            }
        }
    }

    private void AppendPacketAndDerived(SessionEvent packet)
    {
        _store.Append(packet);
        foreach (var derived in _activityDeriver.Process(packet))
        {
            _store.Append(derived);
        }
    }
}
