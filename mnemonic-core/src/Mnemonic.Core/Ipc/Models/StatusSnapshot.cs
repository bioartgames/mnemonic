using Mnemonic.Heuristic;

namespace Mnemonic.Ipc.Models;

public sealed class StatusSnapshot
{
    public int ContractVersion { get; set; } = MnemonicConstants.IpcContractVersion;
    public bool Recording { get; set; }
    public string State { get; set; } = "idle";
    public bool FfmpegOk { get; set; }
    public int CurrentSegmentIndex { get; set; }
    /// <summary>-1 when no manual preserve is queued; otherwise the segment awaiting segment close.</summary>
    public int? PendingManualPreserveSegmentIndex { get; set; }
    public string CapturePrefix { get; set; } = "";
    public string DataRoot { get; set; } = "";
    public string Error { get; set; } = "";
    public int? LastSegmentScore { get; set; }
    public bool? LastSegmentPreserved { get; set; }
    public int? PreserveThreshold { get; set; }
    public int? NotableScoreMin { get; set; }
    public int? HighlightScoreMin { get; set; }
    public List<HeuristicScoreLine>? LastSegmentBreakdown { get; set; }
    public LiveClipPreview? LiveClipPreview { get; set; }
}
