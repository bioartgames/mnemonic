using System.Diagnostics;
using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class FfmpegStderrDrainTests
{
    [Fact]
    public void Drain_highVolumeStderr_processStaysAlive()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var logPath = Path.Combine(Path.GetTempPath(), $"mnemonic-drain-{Guid.NewGuid():N}.log");
        using var process = StartHighVolumeStderrProcess();
        var drain = new FfmpegStderrDrain();
        try
        {
            drain.Start(process, logPath);
            Thread.Sleep(3000);

            Assert.False(process.HasExited);
            Assert.True(File.Exists(logPath));
            Assert.True(new FileInfo(logPath).Length > 10_000);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }

            drain.Dispose();

            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }

    [Fact]
    public void Dispose_completesAfterProcessExit()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var logPath = Path.Combine(Path.GetTempPath(), $"mnemonic-drain-short-{Guid.NewGuid():N}.log");
        using var process = StartShortStderrProcess();
        using var drain = new FfmpegStderrDrain();
        drain.Start(process, logPath);

        Assert.True(process.WaitForExit(5000));
        drain.Dispose();

        Assert.True(File.Exists(logPath));
        var text = File.ReadAllText(logPath);
        Assert.Contains("err", text, StringComparison.OrdinalIgnoreCase);

        File.Delete(logPath);
    }

    private static Process StartHighVolumeStderrProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -Command \"1..3000 | ForEach-Object { [Console]::Error.WriteLine('x' * 100) }; Start-Sleep -Seconds 30\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };

        var process = Process.Start(startInfo);
        Assert.NotNull(process);
        return process;
    }

    private static Process StartShortStderrProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c echo err>&2",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };

        var process = Process.Start(startInfo);
        Assert.NotNull(process);
        return process;
    }
}
