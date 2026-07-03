using System.Text.Json;
using Mnemonic.Events;
using Mnemonic.Retention;

namespace Mnemonic.Git;

public static class GitCommitHashSelector
{
    public static string? Select(IReadOnlyList<SessionEvent> segmentEvents, GitSnapshot snapshot)
    {
        string? last = null;
        foreach (var evt in segmentEvents)
        {
            if (!string.Equals(evt.Type, "git_commit", StringComparison.Ordinal))
            {
                continue;
            }

            var commit = GetExtraString(evt, "commit");
            if (!string.IsNullOrWhiteSpace(commit))
            {
                last = commit;
            }
        }

        if (!string.IsNullOrWhiteSpace(last))
        {
            return last;
        }

        var fromSnapshot = snapshot.Commit.Trim();
        return fromSnapshot.Length > 0 ? fromSnapshot : null;
    }

    private static string? GetExtraString(SessionEvent e, string key)
    {
        if (e.Extra is null || !e.Extra.TryGetValue(key, out var el))
        {
            return null;
        }

        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }
}
