namespace Mnemonic.Git;

public static class GitAheadCountResolver
{
    public static int GetAheadCount(IGitCommandRunner runner, string repoRoot)
    {
        if (!HasUpstream(runner, repoRoot))
        {
            return 0;
        }

        var result = runner.Run(repoRoot, ["rev-list", "--count", "@{u}..HEAD"]);
        if (!result.Ok)
        {
            return 0;
        }

        var text = result.Stdout.Trim();
        return int.TryParse(text, out var count) && count >= 0 ? count : 0;
    }

    public static bool TryParseUpstream(
        IGitCommandRunner runner,
        string repoRoot,
        out string remote,
        out string branch)
    {
        remote = "";
        branch = "";
        if (!HasUpstream(runner, repoRoot))
        {
            return false;
        }

        var result = runner.Run(repoRoot, ["rev-parse", "--abbrev-ref", "@{u}"]);
        if (!result.Ok)
        {
            return false;
        }

        var upstream = result.Stdout.Trim();
        if (upstream.Length == 0)
        {
            return false;
        }

        var slash = upstream.IndexOf('/');
        if (slash <= 0 || slash >= upstream.Length - 1)
        {
            return false;
        }

        remote = upstream[..slash];
        branch = upstream[(slash + 1)..];
        return branch.Length > 0;
    }

    private static bool HasUpstream(IGitCommandRunner runner, string repoRoot)
    {
        var result = runner.Run(repoRoot, ["rev-parse", "--abbrev-ref", "@{u}"]);
        return result.Ok && result.Stdout.Trim().Length > 0;
    }
}
