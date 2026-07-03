namespace Mnemonic.Retention;

public sealed record GitSnapshot(string Commit, string Branch, string Subject)
{
    public static GitSnapshot Empty { get; } = new("", "", "");
}
