using Mnemonic.Events;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ClipTagBuilderTests
{
    [Fact]
    public void SlugBranch_lowercases_and_replaces_slash()
    {
        Assert.Equal("feature-my-branch", ClipTagBuilder.SlugBranch("Feature/My-Branch"));
    }

    [Fact]
    public void SlugBranch_truncates_to_48_chars()
    {
        var longBranch = new string('a', 60);
        Assert.Equal(48, ClipTagBuilder.SlugBranch(longBranch).Length);
    }

    [Fact]
    public void BuildTags_includes_branch_slug_and_event_tags()
    {
        var events = new[]
        {
            SessionEvent.Create(1, "playtest_start"),
            SessionEvent.Create(2, "scene_save"),
            SessionEvent.Create(3, "git_commit"),
            SessionEvent.Create(4, "scene_transition"),
        };

        var tags = ClipTagBuilder.BuildTags(events, "dev/feature", []);

        Assert.Contains("dev-feature", tags);
        Assert.Contains("playtest", tags);
        Assert.Contains("save", tags);
        Assert.Contains("commit", tags);
        Assert.Contains("transition", tags);
    }

    [Fact]
    public void BuildTags_includes_scene_derived_tags()
    {
        var tags = ClipTagBuilder.BuildTags(
            [],
            "",
            ["res://scenes/combat/arena.tscn"]);

        Assert.Contains("combat", tags);
        Assert.Contains("arena", tags);
    }

    [Fact]
    public void BuildTags_adds_error_tag_for_runtime_error()
    {
        var tags = ClipTagBuilder.BuildTags(
            [SessionEvent.Create(1, "runtime_error")],
            "",
            []);

        Assert.Equal(["error"], tags);
    }

    [Fact]
    public void BuildTags_adds_focus_pattern_tags()
    {
        var scriptTags = ClipTagBuilder.BuildTags([SessionEvent.Create(1, "script_focus")], "", []);
        Assert.Contains("script", scriptTags);
        Assert.Contains("edit", scriptTags);

        var layoutTags = ClipTagBuilder.BuildTags([SessionEvent.Create(1, "layout_focus")], "", []);
        Assert.Contains("layout", layoutTags);
        Assert.Contains("transition", layoutTags);
    }

    [Fact]
    public void BuildTags_adds_crg137_signal_tags()
    {
        Assert.Contains("push", ClipTagBuilder.BuildTags([SessionEvent.Create(1, "git_push")], "", []));
        Assert.Contains("debug", ClipTagBuilder.BuildTags([SessionEvent.Create(1, "debug_session_start")], "", []));
        var scriptTags = ClipTagBuilder.BuildTags([SessionEvent.Create(1, "script_save")], "", []);
        Assert.Contains("script", scriptTags);
        Assert.Contains("save", scriptTags);
        Assert.Contains("focus", ClipTagBuilder.BuildTags([SessionEvent.Create(1, "editor_focused_session")], "", []));
    }

    [Fact]
    public void BuildTags_commit_after_playtest_adds_both_tags()
    {
        var tags = ClipTagBuilder.BuildTags([SessionEvent.Create(1, "commit_after_playtest")], "", []);
        Assert.Contains("commit", tags);
        Assert.Contains("commit_after_playtest", tags);
    }

    [Fact]
    public void BuildTags_empty_branch_omits_slug_tag()
    {
        var tags = ClipTagBuilder.BuildTags([SessionEvent.Create(1, "scene_save")], "", []);
        Assert.Equal(["save"], tags);
    }
}
