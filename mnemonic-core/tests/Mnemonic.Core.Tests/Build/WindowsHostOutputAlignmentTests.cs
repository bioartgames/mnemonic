using Xunit;

namespace Mnemonic.Build;

public sealed class WindowsHostOutputAlignmentTests
{
    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void Windows_output_Core_dll_is_as_new_as_project_build(string configuration)
    {
        var repoRoot = FindRepoRoot();
        var coreProjectDll = Path.Combine(
            repoRoot,
            "src",
            "Mnemonic.Core",
            "bin",
            configuration,
            "net8.0",
            "Mnemonic.Core.dll");
        var windowsHostDll = Path.Combine(
            repoRoot,
            "src",
            "Mnemonic.Windows",
            "bin",
            configuration,
            "net8.0-windows",
            "Mnemonic.Core.dll");

        Assert.True(File.Exists(coreProjectDll), $"Missing Core build output: {coreProjectDll}");
        Assert.True(File.Exists(windowsHostDll), $"Missing Windows host output (run dotnet build -c {configuration}): {windowsHostDll}");

        var coreBuilt = File.GetLastWriteTimeUtc(coreProjectDll);
        var hostCopy = File.GetLastWriteTimeUtc(windowsHostDll);
        Assert.True(
            hostCopy >= coreBuilt.AddSeconds(-2),
            $"Stale Mnemonic.Core.dll in Windows output ({hostCopy:o}) vs Core project ({coreBuilt:o}). "
            + $"Rebuild Mnemonic.Windows so Mnemonic launches a host with current Core.");
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void Windows_output_Core_dll_contains_segment_history_writer(string configuration)
    {
        var repoRoot = FindRepoRoot();
        var windowsHostDll = Path.Combine(
            repoRoot,
            "src",
            "Mnemonic.Windows",
            "bin",
            configuration,
            "net8.0-windows",
            "Mnemonic.Core.dll");

        if (!File.Exists(windowsHostDll))
        {
            return;
        }

        var assembly = System.Reflection.Assembly.LoadFrom(windowsHostDll);
        Assert.NotNull(assembly.GetType("Mnemonic.Retention.SegmentHistoryStore", throwOnError: false));
        Assert.NotNull(assembly.GetType("Mnemonic.Retention.SegmentHistoryRecord", throwOnError: false));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Mnemonic.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate mnemonic-core repo root from test output path.");
    }
}
