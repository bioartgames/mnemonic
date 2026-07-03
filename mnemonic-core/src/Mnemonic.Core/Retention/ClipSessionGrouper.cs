using Mnemonic;

namespace Mnemonic.Retention;

public static class ClipSessionGrouper
{
    public static SuggestedGroupsFile Build(IReadOnlyList<ClipIndexEntry> clips, int builtAtUnix)
    {
        var byId = clips.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var unassigned = new HashSet<string>(byId.Keys, StringComparer.Ordinal);
        var groups = new List<SuggestedGroup>();

        GroupSameCommit(byId, unassigned, groups);
        GroupBranchSessions(byId, unassigned, groups);
        GroupPlaytestBlocks(byId, unassigned, groups);
        GroupIterationBlocks(byId, unassigned, groups);
        GroupErrorDebugging(byId, unassigned, groups);
        GroupPostCommit(byId, unassigned, groups);
        GroupSingletons(byId, unassigned, groups);

        groups.Sort((a, b) => MaxCreatedAt(b, byId).CompareTo(MaxCreatedAt(a, byId)));

        return new SuggestedGroupsFile
        {
            GroupsVersion = MnemonicConstants.SuggestedGroupsVersion,
            BuiltAtUnix = builtAtUnix,
            Groups = groups,
        };
    }

    private static void GroupSameCommit(
        Dictionary<string, ClipIndexEntry> byId,
        HashSet<string> unassigned,
        List<SuggestedGroup> groups)
    {
        var byCommit = new Dictionary<string, List<ClipIndexEntry>>(StringComparer.Ordinal);
        foreach (var id in unassigned.ToList())
        {
            var clip = byId[id];
            var commit = clip.GitCommit.Trim();
            if (commit.Length == 0)
            {
                continue;
            }

            if (!byCommit.TryGetValue(commit, out var list))
            {
                list = [];
                byCommit[commit] = list;
            }

            list.Add(clip);
        }

        foreach (var (commit, bucket) in byCommit)
        {
            foreach (var clip in bucket)
            {
                unassigned.Remove(clip.Id);
            }

            var ordered = OrderNewestFirst(bucket);
            var newest = ordered[0];
            var label = TruncateLabel(
                string.IsNullOrWhiteSpace(newest.CommitSubject)
                    ? $"Commit {commit[..Math.Min(8, commit.Length)]}"
                    : newest.CommitSubject);

            groups.Add(new SuggestedGroup
            {
                Id = $"grp_commit_{SanitizeIdPart(commit)}",
                Label = label,
                Reason = SuggestedGroupReason.SameCommit,
                ClipIds = ordered.Select(c => c.Id).ToList(),
            });
        }
    }

    private static void GroupBranchSessions(
        Dictionary<string, ClipIndexEntry> byId,
        HashSet<string> unassigned,
        List<SuggestedGroup> groups)
    {
        var byBranch = new Dictionary<string, List<ClipIndexEntry>>(StringComparer.Ordinal);
        foreach (var id in unassigned)
        {
            var clip = byId[id];
            var branch = clip.GitBranch.Trim();
            if (!byBranch.TryGetValue(branch, out var list))
            {
                list = [];
                byBranch[branch] = list;
            }

            list.Add(clip);
        }

        var gapSeconds = MnemonicConstants.SessionGapHours * 3600;

        foreach (var (branch, branchClips) in byBranch)
        {
            var sorted = branchClips.OrderBy(c => c.CreatedAt).ToList();
            var run = new List<ClipIndexEntry> { sorted[0] };
            for (var i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].CreatedAt - sorted[i - 1].CreatedAt > gapSeconds)
                {
                    TryEmitBranchRun(branch, run, byId, unassigned, groups);
                    run = [sorted[i]];
                }
                else
                {
                    run.Add(sorted[i]);
                }
            }

            TryEmitBranchRun(branch, run, byId, unassigned, groups);
        }
    }

    private static void TryEmitBranchRun(
        string branch,
        List<ClipIndexEntry> run,
        Dictionary<string, ClipIndexEntry> byId,
        HashSet<string> unassigned,
        List<SuggestedGroup> groups)
    {
        if (run.Count < 2)
        {
            return;
        }

        var ordered = OrderNewestFirst(run);
        var newest = ordered[0];
        var date = FormatUtcDate(newest.CreatedAt);
        var branchLabel = string.IsNullOrEmpty(branch) ? "no-branch" : branch;
        var slug = ClipTagBuilder.SlugBranch(branch);
        if (slug.Length == 0)
        {
            slug = "unknown";
        }

        var dateKey = date.Replace("-", "", StringComparison.Ordinal);
        var runIndex = groups.Count(g =>
            g.Reason == SuggestedGroupReason.BranchSession &&
            g.Id.StartsWith($"grp_branch_{slug}_{dateKey}_", StringComparison.Ordinal));

        foreach (var clip in run)
        {
            unassigned.Remove(clip.Id);
        }

        groups.Add(new SuggestedGroup
        {
            Id = $"grp_branch_{slug}_{dateKey}_{runIndex}",
            Label = $"{branchLabel} · {date}",
            Reason = SuggestedGroupReason.BranchSession,
            ClipIds = ordered.Select(c => c.Id).ToList(),
        });
    }

    private static void GroupPlaytestBlocks(
        Dictionary<string, ClipIndexEntry> byId,
        HashSet<string> unassigned,
        List<SuggestedGroup> groups)
    {
        var playtestClips = unassigned
            .Select(id => byId[id])
            .Where(c =>
                HasTag(c, "playtest")
                && !(HasTag(c, "save") && HasTag(c, "playtest")))
            .OrderBy(c => c.CreatedAt)
            .ToList();

        if (playtestClips.Count == 0)
        {
            return;
        }

        var gapSeconds = MnemonicConstants.PlaytestGapMinutes * 60;
        var run = new List<ClipIndexEntry> { playtestClips[0] };
        for (var i = 1; i < playtestClips.Count; i++)
        {
            if (playtestClips[i].CreatedAt - playtestClips[i - 1].CreatedAt > gapSeconds)
            {
                TryEmitPlaytestRun(run, byId, unassigned, groups);
                run = [playtestClips[i]];
            }
            else
            {
                run.Add(playtestClips[i]);
            }
        }

        TryEmitPlaytestRun(run, byId, unassigned, groups);
    }

    private static void TryEmitPlaytestRun(
        List<ClipIndexEntry> run,
        Dictionary<string, ClipIndexEntry> byId,
        HashSet<string> unassigned,
        List<SuggestedGroup> groups)
    {
        if (run.Count < MnemonicConstants.PlaytestMinClipsInGroup)
        {
            return;
        }

        var ordered = OrderNewestFirst(run);
        var newest = ordered[0];
        var dateKey = FormatUtcDate(newest.CreatedAt).Replace("-", "", StringComparison.Ordinal);
        var runIndex = groups.Count(g =>
            g.Reason == SuggestedGroupReason.PlaytestBlock &&
            g.Id.StartsWith($"grp_playtest_{dateKey}_", StringComparison.Ordinal));

        foreach (var clip in run)
        {
            unassigned.Remove(clip.Id);
        }

        groups.Add(new SuggestedGroup
        {
            Id = $"grp_playtest_{dateKey}_{runIndex}",
            Label = $"Playtest · {FormatUtcDate(newest.CreatedAt)}",
            Reason = SuggestedGroupReason.PlaytestBlock,
            ClipIds = ordered.Select(c => c.Id).ToList(),
        });
    }

    private static void GroupIterationBlocks(
        Dictionary<string, ClipIndexEntry> byId,
        HashSet<string> unassigned,
        List<SuggestedGroup> groups)
    {
        var candidates = unassigned
            .Select(id => byId[id])
            .Where(c => HasTag(c, "save") && HasTag(c, "playtest"))
            .OrderBy(c => c.CreatedAt)
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        var gapSeconds = MnemonicConstants.PlaytestGapMinutes * 60;
        EmitTimeRuns(
            candidates,
            gapSeconds,
            groups,
            SuggestedGroupReason.IterationBlock,
            "grp_iteration_",
            date => $"Iteration · {date}",
            (run, dateKey, runIndex) => $"grp_iteration_{dateKey}_{runIndex}",
            unassigned);
    }

    private static void GroupErrorDebugging(
        Dictionary<string, ClipIndexEntry> byId,
        HashSet<string> unassigned,
        List<SuggestedGroup> groups)
    {
        var byDate = new Dictionary<string, List<ClipIndexEntry>>(StringComparer.Ordinal);
        foreach (var id in unassigned.ToList())
        {
            var clip = byId[id];
            if (!HasTag(clip, "error"))
            {
                continue;
            }

            var dateKey = FormatUtcDate(clip.CreatedAt);
            if (!byDate.TryGetValue(dateKey, out var list))
            {
                list = [];
                byDate[dateKey] = list;
            }

            list.Add(clip);
        }

        foreach (var (date, bucket) in byDate)
        {
            if (bucket.Count < 2)
            {
                continue;
            }

            var ordered = OrderNewestFirst(bucket);
            var dateKey = date.Replace("-", "", StringComparison.Ordinal);
            var runIndex = groups.Count(g =>
                g.Reason == SuggestedGroupReason.ErrorDebugging &&
                g.Id.StartsWith($"grp_error_{dateKey}_", StringComparison.Ordinal));

            foreach (var clip in bucket)
            {
                unassigned.Remove(clip.Id);
            }

            groups.Add(new SuggestedGroup
            {
                Id = $"grp_error_{dateKey}_{runIndex}",
                Label = $"Error debugging · {date}",
                Reason = SuggestedGroupReason.ErrorDebugging,
                ClipIds = ordered.Select(c => c.Id).ToList(),
            });
        }
    }

    private static void GroupPostCommit(
        Dictionary<string, ClipIndexEntry> byId,
        HashSet<string> unassigned,
        List<SuggestedGroup> groups)
    {
        var windowSeconds = MnemonicConstants.PostCommitWindowSeconds;

        var tagCandidates = unassigned
            .Select(id => byId[id])
            .Where(c => HasTag(c, "commit_after_playtest"))
            .OrderBy(c => c.CreatedAt)
            .ToList();

        EmitTimeRuns(
            tagCandidates,
            windowSeconds,
            groups,
            SuggestedGroupReason.PostCommit,
            "grp_post_commit_",
            date => $"Post-commit · {date}",
            (run, dateKey, runIndex) => $"grp_post_commit_{dateKey}_{runIndex}",
            unassigned);

        var playtestClips = unassigned
            .Select(id => byId[id])
            .Where(c => HasTag(c, "playtest"))
            .OrderBy(c => c.CreatedAt)
            .ToList();

        var commitClips = unassigned
            .Select(id => byId[id])
            .Where(c => HasTag(c, "commit"))
            .OrderBy(c => c.CreatedAt)
            .ToList();

        if (playtestClips.Count == 0 || commitClips.Count == 0)
        {
            return;
        }

        var parent = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var clip in playtestClips.Concat(commitClips))
        {
            parent[clip.Id] = clip.Id;
        }

        foreach (var play in playtestClips)
        {
            foreach (var commit in commitClips)
            {
                var delta = commit.CreatedAt - play.CreatedAt;
                if (delta <= 0 || delta > windowSeconds)
                {
                    continue;
                }

                Union(parent, play.Id, commit.Id);
            }
        }

        var components = new Dictionary<string, List<ClipIndexEntry>>(StringComparer.Ordinal);
        foreach (var clip in playtestClips.Concat(commitClips))
        {
            var root = Find(parent, clip.Id);
            if (!components.TryGetValue(root, out var list))
            {
                list = [];
                components[root] = list;
            }

            list.Add(clip);
        }

        foreach (var component in components.Values)
        {
            if (component.Count < 2 || component.Count > 10)
            {
                continue;
            }

            if (component.Any(c => !unassigned.Contains(c.Id)))
            {
                continue;
            }

            var ordered = OrderNewestFirst(component);
            var dateKey = FormatUtcDate(ordered[0].CreatedAt).Replace("-", "", StringComparison.Ordinal);
            var runIndex = groups.Count(g =>
                g.Reason == SuggestedGroupReason.PostCommit &&
                g.Id.StartsWith($"grp_post_commit_{dateKey}_", StringComparison.Ordinal));
            foreach (var clip in component)
            {
                unassigned.Remove(clip.Id);
            }

            groups.Add(new SuggestedGroup
            {
                Id = $"grp_post_commit_{dateKey}_{runIndex}",
                Label = $"Post-commit · {FormatUtcDate(ordered[0].CreatedAt)}",
                Reason = SuggestedGroupReason.PostCommit,
                ClipIds = ordered.Select(c => c.Id).ToList(),
            });
        }
    }

    private static void EmitTimeRuns(
        IReadOnlyList<ClipIndexEntry> sorted,
        int gapSeconds,
        List<SuggestedGroup> groups,
        string reason,
        string idPrefix,
        Func<string, string> labelForDate,
        Func<List<ClipIndexEntry>, string, int, string> idForRun,
        HashSet<string> unassigned)
    {
        if (sorted.Count == 0)
        {
            return;
        }

        var run = new List<ClipIndexEntry> { sorted[0] };
        for (var i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].CreatedAt - sorted[i - 1].CreatedAt > gapSeconds)
            {
                TryEmitTimeRun(run, groups, reason, idPrefix, labelForDate, idForRun, unassigned);
                run = [sorted[i]];
            }
            else
            {
                run.Add(sorted[i]);
            }
        }

        TryEmitTimeRun(run, groups, reason, idPrefix, labelForDate, idForRun, unassigned);
    }

    private static void TryEmitTimeRun(
        List<ClipIndexEntry> run,
        List<SuggestedGroup> groups,
        string reason,
        string idPrefix,
        Func<string, string> labelForDate,
        Func<List<ClipIndexEntry>, string, int, string> idForRun,
        HashSet<string> unassigned)
    {
        if (run.Count < 2)
        {
            return;
        }

        var ordered = OrderNewestFirst(run);
        var date = FormatUtcDate(ordered[0].CreatedAt);
        var dateKey = date.Replace("-", "", StringComparison.Ordinal);
        var runIndex = groups.Count(g =>
            g.Reason == reason &&
            g.Id.StartsWith($"{idPrefix}{dateKey}_", StringComparison.Ordinal));

        foreach (var clip in run)
        {
            unassigned.Remove(clip.Id);
        }

        groups.Add(new SuggestedGroup
        {
            Id = idForRun(run, dateKey, runIndex),
            Label = labelForDate(date),
            Reason = reason,
            ClipIds = ordered.Select(c => c.Id).ToList(),
        });
    }

    private static bool HasTag(ClipIndexEntry clip, string tag) =>
        clip.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

    private static string Find(Dictionary<string, string> parent, string id)
    {
        if (parent[id] != id)
        {
            parent[id] = Find(parent, parent[id]);
        }

        return parent[id];
    }

    private static void Union(Dictionary<string, string> parent, string a, string b)
    {
        var rootA = Find(parent, a);
        var rootB = Find(parent, b);
        if (rootA != rootB)
        {
            parent[rootB] = rootA;
        }
    }

    private static void GroupSingletons(
        Dictionary<string, ClipIndexEntry> byId,
        HashSet<string> unassigned,
        List<SuggestedGroup> groups)
    {
        foreach (var id in unassigned.ToList())
        {
            var clip = byId[id];
            var date = FormatUtcDate(clip.CreatedAt);
            var core = string.IsNullOrWhiteSpace(clip.CommitSubject)
                ? clip.Id
                : TruncateLabel(clip.CommitSubject);

            groups.Add(new SuggestedGroup
            {
                Id = $"grp_single_{SanitizeIdPart(clip.Id)}",
                Label = $"{core} · {date}",
                Reason = SuggestedGroupReason.Singleton,
                ClipIds = [clip.Id],
            });
        }
    }

    private static List<ClipIndexEntry> OrderNewestFirst(IReadOnlyList<ClipIndexEntry> clips) =>
        clips.OrderByDescending(c => c.CreatedAt).ToList();

    private static int MaxCreatedAt(SuggestedGroup group, Dictionary<string, ClipIndexEntry> byId)
    {
        var max = 0;
        foreach (var id in group.ClipIds)
        {
            if (byId.TryGetValue(id, out var clip) && clip.CreatedAt > max)
            {
                max = clip.CreatedAt;
            }
        }

        return max;
    }

    private static string FormatUtcDate(int unix) =>
        DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime.ToString("yyyy-MM-dd");

    private static string TruncateLabel(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length <= MnemonicConstants.GroupLabelMaxLen)
        {
            return trimmed;
        }

        return trimmed[..MnemonicConstants.GroupLabelMaxLen];
    }

    private static string SanitizeIdPart(string value)
    {
        var chars = value.Where(c => char.IsLetterOrDigit(c) || c is '_' or '-').ToArray();
        return chars.Length > 0 ? new string(chars) : "unknown";
    }
}
