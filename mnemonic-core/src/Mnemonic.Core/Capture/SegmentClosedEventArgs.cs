namespace Mnemonic.Capture;

public sealed class SegmentClosedEventArgs
{
    public required string CapturePrefix { get; init; }

    public required int Index { get; init; }

    public required string ScratchPath { get; init; }

    public required double TOpenUnix { get; init; }

    public required double TCloseUnix { get; init; }
}
