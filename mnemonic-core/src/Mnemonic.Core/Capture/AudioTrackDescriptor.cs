namespace Mnemonic.Capture;

public sealed class AudioTrackDescriptor
{
    public required string Role { get; init; }

    public required int StreamIndex { get; init; }

    public string Codec { get; init; } = "aac";
}
