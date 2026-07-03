namespace Mnemonic.Ipc.Models;

public sealed class LiveClipPreview
{
    public string CapturePrefix { get; set; } = "";
    public int SegmentIndex { get; set; }
    public string SegmentId { get; set; } = "";
    public double TOpenUnix { get; set; }
    public double TCloseUnix { get; set; }
    public int DurationSeconds { get; set; }
    public string GitBranch { get; set; } = "";
    public string CommitSubject { get; set; } = "";
    public string GitCommit { get; set; } = "";
    public List<string> ScenesActive { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<string> SignalTypes { get; set; } = [];
    public int ScorePreview { get; set; }
}
