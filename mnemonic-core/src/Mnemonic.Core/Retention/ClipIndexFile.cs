namespace Mnemonic.Retention;

public sealed class ClipIndexFile
{
    public int IndexVersion { get; init; }

    public int BuiltAtUnix { get; init; }

    public IReadOnlyList<ClipIndexEntry> Clips { get; init; } = [];
}
