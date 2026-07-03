using System.Diagnostics;

namespace Mnemonic.Git;

public sealed class GitProcessRunner : IGitCommandRunner
{
    private const int TimeoutMs = 5000;

    public GitCommandResult Run(string repoRoot, IReadOnlyList<string> args) =>
        Execute(repoRoot, args, TrimFirstLine);

    public GitCommandResult RunFullOutput(string repoRoot, IReadOnlyList<string> args) =>
        Execute(repoRoot, args, static s => s.Trim());

    private static GitCommandResult Execute(
        string repoRoot,
        IReadOnlyList<string> args,
        Func<string, string> formatStdout)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            startInfo.ArgumentList.Add("-C");
            startInfo.ArgumentList.Add(repoRoot);
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new GitCommandResult(false, -1, "");
            }

            var stdout = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(TimeoutMs))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort.
                }

                return new GitCommandResult(false, -1, "");
            }

            return new GitCommandResult(
                process.ExitCode == 0,
                process.ExitCode,
                formatStdout(stdout));
        }
        catch
        {
            return new GitCommandResult(false, -1, "");
        }
    }

    private static string TrimFirstLine(string stdout)
    {
        var text = stdout.Trim();
        var newline = text.IndexOf('\n', StringComparison.Ordinal);
        if (newline >= 0)
        {
            text = text[..newline].Trim();
        }

        return text;
    }
}
