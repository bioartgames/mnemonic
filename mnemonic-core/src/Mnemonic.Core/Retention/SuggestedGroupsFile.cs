namespace Mnemonic.Retention;

public sealed class SuggestedGroupsFile
{
    public int GroupsVersion { get; init; }

    public int BuiltAtUnix { get; init; }

    public IReadOnlyList<SuggestedGroup> Groups { get; init; } = [];
}
