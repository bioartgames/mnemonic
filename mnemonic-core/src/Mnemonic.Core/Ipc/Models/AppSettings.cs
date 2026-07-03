using Mnemonic.Heuristic;

namespace Mnemonic.Ipc.Models;

public sealed class AppSettings
{
    public int PreserveThreshold { get; set; }
    public int NotableScoreMin { get; set; }
    public int HighlightScoreMin { get; set; }
    public int SegmentHistoryMaxEntries { get; set; }
    public int ScratchCapGb { get; set; }
    public bool CaptureMicEnabled { get; set; }
    public bool CaptureDesktopEnabled { get; set; }
    public string MicDeviceId { get; set; } = "";
    public string DesktopLoopbackDeviceId { get; set; } = "";
    public bool DrawMouse { get; set; }
    public bool StartRecordingOnLaunch { get; set; } = true;
    public int SegmentDurationSeconds { get; set; }
    public string FfmpegPathOverride { get; set; } = "";

    public Dictionary<string, HeuristicTypeSettings>? Heuristics { get; set; }
}
