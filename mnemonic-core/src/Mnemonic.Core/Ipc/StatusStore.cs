using Mnemonic.Heuristic;
using Mnemonic.Ipc.Models;

namespace Mnemonic.Ipc;

public sealed class StatusStore
{
    private readonly DataRootPaths _paths;

    public StatusStore(DataRootPaths paths)
    {
        _paths = paths;
    }

    public void WriteIdle(bool ffmpegOk, string error = "")
    {
        Write(new StatusSnapshot
        {
            ContractVersion = MnemonicConstants.IpcContractVersion,
            Recording = false,
            State = "idle",
            FfmpegOk = ffmpegOk,
            CurrentSegmentIndex = 0,
            DataRoot = _paths.Root,
            Error = error ?? "",
        });
    }

    public void Write(StatusSnapshot incoming, bool preserveLiveClipPreview = true)
    {
        var merged = MergeWithExisting(incoming, preserveLiveClipPreview);
        if (string.IsNullOrEmpty(merged.DataRoot))
        {
            merged.DataRoot = _paths.Root;
        }

        AtomicJsonFile.Write(_paths.StatusFile, merged, JsonOptions.Shared);
    }

    public void WriteRetentionFeedback(
        int score,
        bool preserved,
        int threshold,
        int highlightScoreMin,
        IReadOnlyList<HeuristicScoreLine> breakdown)
    {
        var snapshot = Read() ?? new StatusSnapshot
        {
            ContractVersion = MnemonicConstants.IpcContractVersion,
            DataRoot = _paths.Root,
        };
        snapshot.LastSegmentScore = score;
        snapshot.LastSegmentPreserved = preserved;
        snapshot.PreserveThreshold = threshold;
        snapshot.HighlightScoreMin = highlightScoreMin;
        snapshot.LastSegmentBreakdown = breakdown.ToList();
        Write(snapshot);
    }

    public void WriteLiveClipPreview(LiveClipPreview? preview)
    {
        var snapshot = Read() ?? new StatusSnapshot
        {
            ContractVersion = MnemonicConstants.IpcContractVersion,
            DataRoot = _paths.Root,
        };
        snapshot.LiveClipPreview = preview;
        Write(snapshot, preserveLiveClipPreview: false);
    }

    public StatusSnapshot? Read()
    {
        if (!File.Exists(_paths.StatusFile))
        {
            return null;
        }

        return AtomicJsonFile.Read<StatusSnapshot>(_paths.StatusFile, JsonOptions.Shared);
    }

    private StatusSnapshot MergeWithExisting(StatusSnapshot incoming, bool preserveLiveClipPreview)
    {
        var existing = Read();
        if (existing is null)
        {
            return incoming;
        }

        if (incoming.LastSegmentScore is null)
        {
            incoming.LastSegmentScore = existing.LastSegmentScore;
        }

        if (incoming.LastSegmentPreserved is null)
        {
            incoming.LastSegmentPreserved = existing.LastSegmentPreserved;
        }

        if (incoming.PreserveThreshold is null)
        {
            incoming.PreserveThreshold = existing.PreserveThreshold;
        }

        if (incoming.HighlightScoreMin is null)
        {
            incoming.HighlightScoreMin = existing.HighlightScoreMin;
        }

        if (incoming.NotableScoreMin is null)
        {
            incoming.NotableScoreMin = existing.NotableScoreMin;
        }

        if (incoming.LastSegmentBreakdown is null)
        {
            incoming.LastSegmentBreakdown = existing.LastSegmentBreakdown;
        }

        if (preserveLiveClipPreview && incoming.LiveClipPreview is null)
        {
            incoming.LiveClipPreview = existing.LiveClipPreview;
        }

        if (!incoming.PendingManualPreserveSegmentIndex.HasValue)
        {
            incoming.PendingManualPreserveSegmentIndex = existing.PendingManualPreserveSegmentIndex;
        }

        return incoming;
    }

    public void SetPendingManualPreserve(int segmentIndex)
    {
        if (segmentIndex < 0)
        {
            return;
        }

        var snapshot = Read() ?? new StatusSnapshot
        {
            ContractVersion = MnemonicConstants.IpcContractVersion,
            DataRoot = _paths.Root,
        };
        snapshot.PendingManualPreserveSegmentIndex = segmentIndex;
        Write(snapshot);
    }

    public void ClearPendingManualPreserve(int segmentIndex)
    {
        var snapshot = Read();
        if (snapshot is null)
        {
            return;
        }

        if (snapshot.PendingManualPreserveSegmentIndex != segmentIndex)
        {
            return;
        }

        snapshot.PendingManualPreserveSegmentIndex = -1;
        Write(snapshot);
    }
}
