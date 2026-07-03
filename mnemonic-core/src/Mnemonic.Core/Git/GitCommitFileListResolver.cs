using Mnemonic;

namespace Mnemonic.Git;

public sealed class GitCommitFileListResolver(IGitCommandRunner runner, string? repoRoot)
{
    public IReadOnlyList<string> Resolve(string? commitHash)
    {
        if (string.IsNullOrWhiteSpace(repoRoot) || string.IsNullOrWhiteSpace(commitHash))
        {
            return [];
        }

        var result = runner.RunFullOutput(
            repoRoot,
            ["show", "--name-only", "--pretty=format:", commitHash]);
        if (!result.Ok || string.IsNullOrWhiteSpace(result.Stdout))
        {
            return [];
        }

        return ParsePaths(result.Stdout);
    }

    private static IReadOnlyList<string> ParsePaths(string stdout)
    {
        var paths = new List<string>();
        foreach (var line in stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var path = line.Trim();
            if (path.Length == 0)
            {
                continue;
            }

            if (path.Length > MnemonicConstants.MaxFilesModifiedPathLength)
            {
                continue;
            }

            paths.Add(path);
            if (paths.Count >= MnemonicConstants.MaxFilesModifiedCount)
            {
                break;
            }
        }

        return paths;
    }
}
