using Mnemonic.Capture;
using Mnemonic.Events;
using Mnemonic.Ipc.Models;

namespace Mnemonic.Retention;

public sealed class LiveClipPreviewBuilder
{
    private readonly Func<bool> _isRecording;
    private readonly Func<(bool Ok, double TOpenUnix, double TCloseUnix)> _currentWindow;
    private readonly Func<int> _currentSegmentIndex;
    private readonly Func<string?> _getCapturePrefix;
    private readonly EventIngestService _eventIngest;
    private readonly GitSnapshotProvider _gitSnapshot;
    private readonly AppSettings _settings;

    public delegate GitSnapshot GitSnapshotProvider();

    public LiveClipPreviewBuilder(
        CaptureService capture,
        EventIngestService eventIngest,
        GitSnapshotProvider gitSnapshot,
        AppSettings settings)
        : this(
            () => capture.IsRecording,
            () =>
            {
                var ok = capture.TryGetCurrentSegmentWindow(out var tOpenUnix, out var tCloseUnix);
                return (ok, tOpenUnix, tCloseUnix);
            },
            () => capture.CurrentSegmentIndex,
            () => capture.ActiveCapturePrefix,
            eventIngest,
            gitSnapshot,
            settings)
    {
    }

    internal LiveClipPreviewBuilder(
        Func<bool> isRecording,
        Func<(bool Ok, double TOpenUnix, double TCloseUnix)> currentWindow,
        Func<int> currentSegmentIndex,
        Func<string?> getCapturePrefix,
        EventIngestService eventIngest,
        GitSnapshotProvider gitSnapshot,
        AppSettings settings)
    {
        _isRecording = isRecording;
        _currentWindow = currentWindow;
        _currentSegmentIndex = currentSegmentIndex;
        _getCapturePrefix = getCapturePrefix;
        _eventIngest = eventIngest;
        _gitSnapshot = gitSnapshot;
        _settings = settings;
    }

    public LiveClipPreview? Build()
    {
        if (!_isRecording())
        {
            return null;
        }

        _eventIngest.Poll();

        var (ok, tOpenUnix, tCloseUnix) = _currentWindow();
        if (!ok)
        {
            return null;
        }

        var segmentIndex = _currentSegmentIndex();
        if (segmentIndex < 0)
        {
            return null;
        }

        var events = _eventIngest.GetScoringEvents(tOpenUnix, tCloseUnix);
        var editorScenes = EditorSceneSnapshotReader.TryRead(_eventIngest.Paths);
        var scenes = ClipSceneExtractor.BuildScenesActive(events, editorScenes).ToList();
        var git = _gitSnapshot();
        var tags = ClipTagBuilder.BuildTags(events, git.Branch, scenes).ToList();
        var signalTypes = events
            .Select(e => e.Type)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();
        var score = _eventIngest.ScoreWindow(tOpenUnix, tCloseUnix);

        var capturePrefix = _getCapturePrefix() ?? "";
        return new LiveClipPreview
        {
            CapturePrefix = capturePrefix,
            SegmentIndex = segmentIndex,
            SegmentId = string.IsNullOrEmpty(capturePrefix)
                ? $"segment_{segmentIndex:D5}"
                : ClipIdentity.FormatClipId(capturePrefix, segmentIndex),
            TOpenUnix = tOpenUnix,
            TCloseUnix = tCloseUnix,
            DurationSeconds = SegmentDurationPolicy.Normalize(_settings.SegmentDurationSeconds),
            GitBranch = git.Branch,
            CommitSubject = git.Subject,
            GitCommit = git.Commit,
            ScenesActive = scenes,
            Tags = tags,
            SignalTypes = signalTypes,
            ScorePreview = score,
        };
    }
}
