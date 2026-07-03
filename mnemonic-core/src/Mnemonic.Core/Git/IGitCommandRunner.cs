namespace Mnemonic.Git;

public interface IGitCommandRunner
{
    GitCommandResult Run(string repoRoot, IReadOnlyList<string> args);

    GitCommandResult RunFullOutput(string repoRoot, IReadOnlyList<string> args);
}
