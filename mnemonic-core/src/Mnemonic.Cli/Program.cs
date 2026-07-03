using Mnemonic.Ipc;
using Mnemonic.Retention;

namespace Mnemonic.Cli;

internal static class Program
{
    static int Main(string[] args)
    {
        if (args is ["--help"] or ["-h"] or [])
        {
            PrintHelp();
            return 0;
        }

        if (args is ["rebuild-clips-index"])
        {
            return RebuildClipsIndex(null);
        }

        if (args is ["rebuild-clips-index", var dataRoot])
        {
            return RebuildClipsIndex(dataRoot);
        }

        Console.Error.WriteLine($"Unknown command: {args[0]}");
        PrintHelp();
        return 1;
    }

    private static int RebuildClipsIndex(string? dataRoot)
    {
        var paths = string.IsNullOrWhiteSpace(dataRoot)
            ? new DataRootPaths()
            : new DataRootPaths(dataRoot);
        try
        {
            new ClipIndexService(paths).Rebuild();
            Console.WriteLine($"Wrote {paths.ClipsIndexFile}");
            Console.WriteLine($"Wrote {paths.SuggestedGroupsFile}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"rebuild-clips-index failed: {ex.Message}");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine($"{MnemonicConstants.ProductName} CLI");
        Console.WriteLine("  rebuild-clips-index [data_root]");
        Console.WriteLine("    Rebuild control/clips_index.json and control/suggested_groups.json.");
    }
}
