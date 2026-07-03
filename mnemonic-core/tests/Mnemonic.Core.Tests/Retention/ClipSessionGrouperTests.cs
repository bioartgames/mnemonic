using Mnemonic;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ClipSessionGrouperTests
{
    [Fact]
    public void Build_empty_returns_empty_groups()
    {
        var result = ClipSessionGrouper.Build([], 1000);
        Assert.Equal(MnemonicConstants.SuggestedGroupsVersion, result.GroupsVersion);
        Assert.Equal(1000, result.BuiltAtUnix);
        Assert.Empty(result.Groups);
    }

    [Fact]
    public void Build_same_commit_groups_two_clips()
    {
        var clips = new[]
        {
            Entry("segment_00001", 100, "abc123def", "main", "Fix capture", []),
            Entry("segment_00002", 200, "abc123def", "main", "Fix capture", []),
        };

        var result = ClipSessionGrouper.Build(clips, 1000);
        Assert.Single(result.Groups);
        Assert.Equal(SuggestedGroupReason.SameCommit, result.Groups[0].Reason);
        Assert.Equal(2, result.Groups[0].ClipIds.Count);
        Assert.Equal("segment_00002", result.Groups[0].ClipIds[0]);
        Assert.Equal("segment_00001", result.Groups[0].ClipIds[1]);
    }

    [Fact]
    public void Build_branch_session_splits_on_gap()
    {
        var baseTime = 1_000_000;
        var clips = new[]
        {
            Entry("segment_00001", baseTime, "", "dev/feature", "", []),
            Entry("segment_00002", baseTime + 7200, "", "dev/feature", "", []),
            Entry("segment_00003", baseTime + 7200 + 18_000, "", "dev/feature", "", []),
        };

        var result = ClipSessionGrouper.Build(clips, 1000);
        var branchGroups = result.Groups.Where(g => g.Reason == SuggestedGroupReason.BranchSession).ToList();
        Assert.Single(branchGroups);
        Assert.Equal(2, branchGroups[0].ClipIds.Count);
        Assert.Contains("segment_00001", branchGroups[0].ClipIds);
        Assert.Contains("segment_00002", branchGroups[0].ClipIds);

        var singleton = result.Groups.Single(g => g.Reason == SuggestedGroupReason.Singleton);
        Assert.Equal("segment_00003", singleton.ClipIds[0]);
    }

    [Fact]
    public void Build_playtest_block_requires_two()
    {
        var baseTime = 2_000_000;
        var clips = new[]
        {
            Entry("segment_00001", baseTime, "", "branch-a", "", ["playtest"]),
            Entry("segment_00002", baseTime + 1800, "", "branch-b", "", ["playtest"]),
            Entry("segment_00003", baseTime + 10_000, "", "branch-c", "", ["playtest"]),
        };

        var result = ClipSessionGrouper.Build(clips, 1000);
        var playtest = result.Groups.Where(g => g.Reason == SuggestedGroupReason.PlaytestBlock).ToList();
        Assert.Single(playtest);
        Assert.Equal(2, playtest[0].ClipIds.Count);
        Assert.Contains("segment_00001", playtest[0].ClipIds);
        Assert.Contains("segment_00002", playtest[0].ClipIds);

        var singletons = result.Groups.Where(g => g.Reason == SuggestedGroupReason.Singleton).ToList();
        Assert.Single(singletons);
        Assert.Equal("segment_00003", singletons[0].ClipIds[0]);
    }

    [Fact]
    public void Build_iteration_block_groups_save_and_playtest_within_gap()
    {
        var baseTime = 3_000_000;
        var clips = new[]
        {
            Entry("segment_00001", baseTime, "", "branch-a", "", ["save", "playtest"]),
            Entry("segment_00002", baseTime + 1800, "", "branch-b", "", ["save", "playtest"]),
        };

        var result = ClipSessionGrouper.Build(clips, 1000);
        var iteration = result.Groups.Single(g => g.Reason == SuggestedGroupReason.IterationBlock);
        Assert.Equal(2, iteration.ClipIds.Count);
        Assert.StartsWith("Iteration ·", iteration.Label);
    }

    [Fact]
    public void Build_error_debugging_groups_same_utc_day()
    {
        var dayStart = new DateTimeOffset(2025, 5, 20, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var clips = new[]
        {
            Entry("segment_00001", (int)dayStart, "", "branch-a", "", ["error"]),
            Entry("segment_00002", (int)dayStart + 3600, "", "branch-b", "", ["error"]),
        };

        var result = ClipSessionGrouper.Build(clips, 1000);
        var group = result.Groups.Single(g => g.Reason == SuggestedGroupReason.ErrorDebugging);
        Assert.Equal(2, group.ClipIds.Count);
        Assert.Contains("Error debugging", group.Label);
    }

    [Fact]
    public void Build_post_commit_tag_pass_groups_commit_after_playtest()
    {
        var baseTime = 4_000_000;
        var clips = new[]
        {
            Entry("segment_00001", baseTime, "", "branch-a", "", ["commit_after_playtest"]),
            Entry("segment_00002", baseTime + 600, "", "branch-b", "", ["commit_after_playtest"]),
        };

        var result = ClipSessionGrouper.Build(clips, 1000);
        var group = result.Groups.Single(g => g.Reason == SuggestedGroupReason.PostCommit);
        Assert.Equal(2, group.ClipIds.Count);
    }

    [Fact]
    public void Build_post_commit_temporal_links_playtest_and_commit()
    {
        var baseTime = 5_000_000;
        var clips = new[]
        {
            Entry("segment_00001", baseTime, "", "branch-a", "", ["playtest"]),
            Entry("segment_00002", baseTime + 900, "", "branch-b", "", ["commit"]),
        };

        var result = ClipSessionGrouper.Build(clips, 1000);
        var group = result.Groups.Single(g => g.Reason == SuggestedGroupReason.PostCommit);
        Assert.Equal(2, group.ClipIds.Count);
        Assert.Contains("segment_00001", group.ClipIds);
        Assert.Contains("segment_00002", group.ClipIds);
    }

    [Fact]
    public void Build_clip_assigned_once()
    {
        var clips = new[]
        {
            Entry("segment_00001", 100, "c1", "main", "A", ["playtest"]),
            Entry("segment_00002", 200, "c1", "main", "A", []),
            Entry("segment_00003", 300, "", "other", "", []),
        };

        var result = ClipSessionGrouper.Build(clips, 1000);
        var allIds = result.Groups.SelectMany(g => g.ClipIds).OrderBy(id => id).ToList();
        Assert.Equal(["segment_00001", "segment_00002", "segment_00003"], allIds);
    }

    private static ClipIndexEntry Entry(
        string id,
        int createdAt,
        string gitCommit,
        string gitBranch,
        string commitSubject,
        string[] tags) =>
        new()
        {
            Id = id,
            CreatedAt = createdAt,
            DurationSeconds = 60,
            Score = 5,
            GitCommit = gitCommit,
            GitBranch = gitBranch,
            CommitSubject = commitSubject,
            ScenesActive = [],
            Tags = tags,
            HasVideo = false,
            HasThumb = false,
        };
}
