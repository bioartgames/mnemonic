using Mnemonic.Events;

namespace Mnemonic.Git;

public static class JsonlGitEventDeduper
{
    public static bool HasRecentGitCommit(string path, string commit)
    {
        foreach (var evt in ReadTailEvents(path))
        {
            if (evt.Type != "git_commit")
            {
                continue;
            }

            if (TryGetExtraString(evt, "commit", out var existing) &&
                string.Equals(existing, commit, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasRecentGitBranchChange(string path, string branch)
    {
        foreach (var evt in ReadTailEvents(path))
        {
            if (evt.Type != "git_branch_change")
            {
                continue;
            }

            if (TryGetExtraString(evt, "branch", out var existing) &&
                string.Equals(existing, branch, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasRecentGitPush(string path, string branch, double windowSeconds)
    {
        var cutoff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (long)Math.Ceiling(windowSeconds);
        foreach (var evt in ReadTailEvents(path))
        {
            if (evt.Type != "git_push" || evt.T < cutoff)
            {
                continue;
            }

            if (TryGetExtraString(evt, "branch", out var existing) &&
                string.Equals(existing, branch, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<SessionEvent> ReadTailEvents(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        var fileInfo = new FileInfo(path);
        var readLength = (int)Math.Min(fileInfo.Length, MnemonicConstants.GitJsonlDedupeTailBytes);
        if (readLength <= 0)
        {
            yield break;
        }

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(-readLength, SeekOrigin.End);

        var buffer = new byte[readLength];
        var offset = 0;
        while (offset < readLength)
        {
            var read = stream.Read(buffer, offset, readLength - offset);
            if (read <= 0)
            {
                break;
            }

            offset += read;
        }

        var text = System.Text.Encoding.UTF8.GetString(buffer);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            if (SessionEvent.TryParseFromJsonLine(lines[i].TrimEnd('\r'), out var evt) && evt is not null)
            {
                yield return evt;
            }
        }
    }

    private static bool TryGetExtraString(SessionEvent evt, string key, out string value)
    {
        value = "";
        if (evt.Extra is null || !evt.Extra.TryGetValue(key, out var element))
        {
            return false;
        }

        value = element.GetString() ?? "";
        return true;
    }
}
