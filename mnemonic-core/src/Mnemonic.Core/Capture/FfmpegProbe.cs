using System.Diagnostics;

namespace Mnemonic.Capture;

public static class FfmpegProbe
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

    public static bool TryVerify(string executablePath, out string errorMessage)
    {
        errorMessage = "";

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = "-version",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            if (!process.Start())
            {
                errorMessage = "failed to start process";
                return false;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        if (!process.WaitForExit(ProbeTimeout))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort.
            }

            errorMessage = "timed out after 5s";
            return false;
        }

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            errorMessage = $"exit code {process.ExitCode}: {Truncate(stderr, 200)}";
            return false;
        }

        return true;
    }

    public static bool TryVerifyCaptureCapabilities(string executablePath, out string errorMessage)
    {
        if (!TryVerify(executablePath, out errorMessage))
        {
            return false;
        }

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = "-hide_banner -h filter=gfxcapture",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            if (!process.Start())
            {
                errorMessage = "failed to start process for filter probe";
                return false;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(ProbeTimeout))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort.
            }

            errorMessage = "filter probe timed out after 5s";
            return false;
        }

        var output = stdoutTask.GetAwaiter().GetResult() + stderrTask.GetAwaiter().GetResult();
        if (output.Contains("gfxcapture", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (output.Contains("ddagrab", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage =
                "FFmpeg has ddagrab but not gfxcapture. Bundled FFmpeg is too old; re-run scripts/fetch-ffmpeg.ps1.";
            return false;
        }

        errorMessage = "FFmpeg missing gfxcapture filter (required for monitor capture).";
        return false;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[..maxLength];
    }
}
