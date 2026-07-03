namespace Mnemonic.Retention;

public sealed class SuggestedGroup
{
    public required string Id { get; init; }

    public required string Label { get; init; }

    public required string Reason { get; init; }

    public required IReadOnlyList<string> ClipIds { get; init; }
}
