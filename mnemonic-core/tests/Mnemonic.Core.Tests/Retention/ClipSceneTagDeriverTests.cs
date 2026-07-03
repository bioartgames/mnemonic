using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ClipSceneTagDeriverTests
{
    [Fact]
    public void DeriveTags_includes_basename_and_folder_segments()
    {
        var tags = ClipSceneTagDeriver.DeriveTags(["res://scenes/combat/arena.tscn"]);

        Assert.Contains("arena", tags);
        Assert.Contains("combat", tags);
        Assert.DoesNotContain("scenes", tags);
    }

    [Fact]
    public void DeriveTags_dedupes_case_insensitive()
    {
        var tags = ClipSceneTagDeriver.DeriveTags(
        [
            "res://combat/Arena.tscn",
            "res://other/combat/level.tscn",
        ]);

        Assert.Single(tags, t => t == "combat");
    }

    [Fact]
    public void DeriveTags_caps_at_twelve()
    {
        var paths = new List<string>();
        for (var i = 0; i < 20; i++)
        {
            paths.Add($"res://areas/area{i}/scene{i}.tscn");
        }

        Assert.True(ClipSceneTagDeriver.DeriveTags(paths).Count <= 12);
    }

    [Fact]
    public void SlugSegment_replaces_non_alphanumeric_with_hyphen()
    {
        Assert.Equal("boss-room", ClipSceneTagDeriver.SlugSegment("Boss Room"));
    }
}
