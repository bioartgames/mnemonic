using Mnemonic.Git;
using Xunit;

namespace Mnemonic.Core.Tests.Git;

public sealed class GitRepositoryLocatorTests
{
    [Fact]
    public void FindRepoRootFrom_finds_dot_git()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_git_{Guid.NewGuid():N}");
        var repoDir = Path.Combine(root, "a", "b");
        try
        {
            Directory.CreateDirectory(repoDir);
            File.WriteAllText(Path.Combine(repoDir, ".git"), "");

            Assert.Equal(repoDir, GitRepositoryLocator.FindRepoRootFrom(repoDir));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void FindRepoRootFrom_returns_null_when_missing()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_git_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(dir);
            Assert.Null(GitRepositoryLocator.FindRepoRootFrom(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
