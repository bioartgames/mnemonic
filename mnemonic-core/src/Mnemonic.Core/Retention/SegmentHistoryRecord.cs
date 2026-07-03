using Mnemonic.Heuristic;

namespace Mnemonic.Retention;

public sealed class SegmentHistoryRecord
{
    public int ContractVersion { get; set; } = global::Mnemonic.MnemonicConstants.SegmentHistoryContractVersion;

    public int SegmentIndex { get; set; }

    public string CapturePrefix { get; set; } = "";

    public string ClipId { get; set; } = "";

    public double TOpenUnix { get; set; }

    public double TCloseUnix { get; set; }

    public int SegmentDurationSeconds { get; set; }

    public int Score { get; set; }

    public int Threshold { get; set; }

    public bool Preserved { get; set; }

    public bool ManualPreserve { get; set; }

    public List<HeuristicScoreLine> Breakdown { get; set; } = [];

    public string GitBranch { get; set; } = "";

    public string GitCommit { get; set; } = "";

    public string GitSubject { get; set; } = "";
}
