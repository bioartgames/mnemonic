using System.Diagnostics;

namespace Mnemonic.Capture;

/// <summary>
/// Kills orphaned FFmpeg capture processes launched from the Mnemonic bundle path.
/// </summary>
public static class FfmpegProcessCleanup
{
    public static void KillBundledOrphans(string? bundledExecutablePath)
    {
        if (string.IsNullOrWhiteSpace(bundledExecutablePath) || !File.Exists(bundledExecutablePath))
        {
            return;
        }

        var expectedPath = Path.GetFullPath(bundledExecutablePath);
        foreach (var process in Process.GetProcessesByName("ffmpeg"))
        {
            using (process)
            {
                if (!TryGetExecutablePath(process, out var actualPath))
                {
                    continue;
                }

                if (!PathsEqual(actualPath, expectedPath))
                {
                    continue;
                }

                TryKillProcessTree(process);
            }
        }
    }

    private static bool TryGetExecutablePath(Process process, out string path)
    {
        path = "";
        try
        {
            path = process.MainModule?.FileName ?? "";
            return path.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(
            Path.GetFullPath(a),
            Path.GetFullPath(b),
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
}
