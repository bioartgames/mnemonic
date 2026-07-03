namespace Mnemonic.Capture;

internal sealed class CaptureSessionState
{
    public required string Prefix { get; init; }

    public long CaptureStartUnix { get; init; }

    public int LastScratchMaxIndex { get; set; } = -1;

    public int CurrentSegmentIndex { get; set; }

    public string? FfmpegLogPath { get; set; }

    public FfmpegStderrDrain? StderrDrain { get; set; }

    public List<WasapiPipePump>? AudioPumps { get; set; }

    public CancellationTokenSource? AudioPumpCts { get; set; }

    public HashSet<int> EmittedCloseIndices { get; } = new();
}
