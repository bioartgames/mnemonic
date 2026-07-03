namespace Mnemonic.Git;

public sealed record GitCommandResult(bool Ok, int ExitCode, string Stdout);
