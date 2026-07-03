namespace Mnemonic.Git;

public static class GitRepositoryLocator
{
    private static string? _cachedRepoRoot;

    public static string? FindRepoRoot()
    {
        if (_cachedRepoRoot is not null)
        {
            return _cachedRepoRoot;
        }

        var fromCwd = FindRepoRootFrom(Environment.CurrentDirectory);
        if (fromCwd is not null)
        {
            _cachedRepoRoot = fromCwd;
            return _cachedRepoRoot;
        }

        var fromBase = FindRepoRootFrom(AppContext.BaseDirectory);
        if (fromBase is not null)
        {
            _cachedRepoRoot = fromBase;
        }

        return _cachedRepoRoot;
    }

    internal static string? FindRepoRootFrom(string startDirectory)
    {
        var current = Path.GetFullPath(startDirectory);
        while (true)
        {
            var gitPath = Path.Combine(current, ".git");
            if (File.Exists(gitPath) || Directory.Exists(gitPath))
            {
                return current;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                return null;
            }

            current = parent.FullName;
        }
    }
}
