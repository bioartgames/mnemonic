using System.Diagnostics;
using Mnemonic.Capture;
using Mnemonic.Events;
using Mnemonic.Git;
using Mnemonic.Heuristic;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;

namespace Mnemonic.Retention;

public sealed class ClipPreserver
{
    private const int ClipJsonRetryCount = 2;
    private const int ClipJsonRetryDelayMs = 75;
    private const int ThumbRetryCount = 4;
    private const int ThumbRetryDelayMs = 150;

    private readonly DataRootPaths _paths;
    private readonly ManualPreserveTracker _tracker;
    private readonly EventIngestService _eventIngest;
    private readonly GitPollService _gitPoll;
    private readonly AppSettings _settings;
    private readonly FfmpegResolution? _ffmpeg;
    private readonly ClipIndexService _clipIndex;
    private readonly StatusStore _statusStore;
    private readonly SegmentHistoryStore _segmentHistoryStore;
    private readonly GitCommitFileListResolver _fileListResolver;

    public ClipPreserver(
        DataRootPaths paths,
        ManualPreserveTracker tracker,
        EventIngestService eventIngest,
        GitPollService gitPoll,
        AppSettings settings,
        ClipIndexService clipIndex,
        StatusStore statusStore,
        FfmpegResolution? ffmpeg = null,
        SegmentHistoryStore? segmentHistoryStore = null,
        GitCommitFileListResolver? fileListResolver = null)
    {
        _paths = paths;
        _tracker = tracker;
        _eventIngest = eventIngest;
        _gitPoll = gitPoll;
        _settings = settings;
        _clipIndex = clipIndex;
        _statusStore = statusStore;
        _ffmpeg = ffmpeg;
        _segmentHistoryStore = segmentHistoryStore ?? new SegmentHistoryStore();
        _fileListResolver = fileListResolver
            ?? new GitCommitFileListResolver(gitPoll.CommandRunner, gitPoll.RepositoryRoot);
    }

    public void HandleSegmentClosed(SegmentClosedEventArgs args)
    {
        var threshold = PreserveThresholdPolicy.Clamp(_settings.PreserveThreshold);
        var highlightMin = ScoreTierNormalizer.ResolveHighlightMin(threshold, _settings.HighlightScoreMin);

        _eventIngest.Poll();
        var segmentEvents = _eventIngest.GetScoringEvents(args.TOpenUnix, args.TCloseUnix);
        var (score, breakdown) = HeuristicScorer.ScoreBreakdown(segmentEvents, _settings);
        var manualFlag = _tracker.ShouldPreserve(args.Index);
        var emitOngoing = segmentEvents.Any(e => e.Type == "playtest_ongoing");
        var preserve = score >= threshold || manualFlag || emitOngoing;
        _statusStore.WriteRetentionFeedback(score, preserve, threshold, highlightMin, breakdown);

        var preserveSucceeded = false;
        if (preserve)
        {
            preserveSucceeded = PreserveScratchSegment(args, score);
            if (!preserveSucceeded)
            {
                Trace.WriteLine(
                    $"Mnemonic: preserve failed for {ClipIdentity.FormatClipId(args.CapturePrefix, args.Index)}: scratch video missing");
            }
        }
        else
        {
            DiscardScratchSegment(args.ScratchPath);
        }

        AppendSegmentHistory(args, score, threshold, manualFlag, preserve, preserveSucceeded, breakdown);

        _tracker.Clear(args.Index);
        _statusStore.ClearPendingManualPreserve(args.Index);
    }

    private void AppendSegmentHistory(
        SegmentClosedEventArgs args,
        int score,
        int threshold,
        bool manualFlag,
        bool preserve,
        bool preserveSucceeded,
        IReadOnlyList<HeuristicScoreLine> breakdown)
    {
        var git = _gitPoll.CurrentSnapshot;
        var clipId = preserve && preserveSucceeded
            ? ClipIdentity.FormatClipId(args.CapturePrefix, args.Index)
            : "";
        var record = new SegmentHistoryRecord
        {
            SegmentIndex = args.Index,
            CapturePrefix = args.CapturePrefix,
            ClipId = clipId,
            TOpenUnix = args.TOpenUnix,
            TCloseUnix = args.TCloseUnix,
            SegmentDurationSeconds = SegmentDurationPolicy.Normalize(_settings.SegmentDurationSeconds),
            Score = score,
            Threshold = threshold,
            Preserved = preserve,
            ManualPreserve = manualFlag,
            Breakdown = breakdown.ToList(),
            GitBranch = git.Branch,
            GitCommit = git.Commit,
            GitSubject = git.Subject,
        };
        var maxEntries = SegmentHistoryMaxEntriesPolicy.Clamp(_settings.SegmentHistoryMaxEntries);
        _segmentHistoryStore.Append(_paths.SegmentHistoryFile, record, maxEntries);
    }

    private bool PreserveScratchSegment(SegmentClosedEventArgs args, int score)
    {
        if (!File.Exists(args.ScratchPath))
        {
            return false;
        }

        var clipId = ClipIdentity.FormatClipId(args.CapturePrefix, args.Index);
        var clipDir = Path.Combine(_paths.ClipsDir, clipId);
        if (Directory.Exists(clipDir))
        {
            throw new IOException($"Refusing to overwrite existing clip folder: {clipDir}");
        }

        Directory.CreateDirectory(clipDir);
        var destPath = Path.Combine(clipDir, MnemonicConstants.ClipVideoFileName);
        File.Move(args.ScratchPath, destPath);

        if (WriteClipJsonWithRetry(clipDir, args, score))
        {
            TryWriteThumbnail(clipDir, destPath);
            _clipIndex.Rebuild();
        }

        return true;
    }

    private void TryWriteThumbnail(string clipDir, string videoPath)
    {
        if (_ffmpeg is not { IsAvailable: true, ExecutablePath: not null })
        {
            return;
        }

        var thumbPath = Path.Combine(clipDir, MnemonicConstants.ClipThumbFileName);
        if (!ClipThumbnailGenerator.TryGenerateWithRetry(
                _ffmpeg.ExecutablePath,
                videoPath,
                thumbPath,
                MnemonicConstants.ClipThumbWidthPx,
                MnemonicConstants.ClipThumbHeightPx,
                ThumbRetryCount,
                ThumbRetryDelayMs))
        {
            Trace.WriteLine($"Mnemonic: thumbnail generation failed for {clipDir} after {ThumbRetryCount} attempts");
        }
    }

    private bool WriteClipJsonWithRetry(string clipDir, SegmentClosedEventArgs args, int score)
    {
        var events = _eventIngest.EventsForSegment(args.TOpenUnix, args.TCloseUnix);
        var editorScenes = EditorSceneSnapshotReader.TryRead(_paths);
        var segmentSeconds = SegmentDurationPolicy.Normalize(_settings.SegmentDurationSeconds);
        var commitHash = GitCommitHashSelector.Select(events, _gitPoll.CurrentSnapshot);
        var filesModified = _fileListResolver.Resolve(commitHash);
        var request = new ClipWriteRequest(
            args.CapturePrefix,
            args.Index,
            args.TOpenUnix,
            args.TCloseUnix,
            segmentSeconds,
            score,
            events,
            CaptureAudioConfig.FromSettings(_settings),
            _gitPoll.CurrentSnapshot,
            editorScenes,
            filesModified);

        for (var attempt = 0; attempt < ClipJsonRetryCount; attempt++)
        {
            try
            {
                ClipJsonWriter.Write(clipDir, request);
                return true;
            }
            catch (Exception) when (attempt < ClipJsonRetryCount - 1)
            {
                Thread.Sleep(ClipJsonRetryDelayMs);
            }
        }

        return false;
    }

    private static void DiscardScratchSegment(string scratchPath)
    {
        if (!File.Exists(scratchPath))
        {
            return;
        }

        File.Delete(scratchPath);
    }
}
