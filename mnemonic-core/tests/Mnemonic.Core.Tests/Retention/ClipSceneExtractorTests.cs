using Mnemonic.Events;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ClipSceneExtractorTests
{
    [Fact]
    public void BuildScenesActive_dedupes_transitions_in_order()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":1,\"type\":\"scene_transition\",\"to_scene\":\"res://a.tscn\"}",
                out var a));
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":2,\"type\":\"scene_transition\",\"to_scene\":\"res://b.tscn\"}",
                out var b));
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":3,\"type\":\"scene_transition\",\"to_scene\":\"res://a.tscn\"}",
                out var c));

        var scenes = ClipSceneExtractor.BuildScenesActive([a!, b!, c!]);

        Assert.Equal(2, scenes.Count);
        Assert.Equal("res://a.tscn", scenes[0]);
        Assert.Equal("res://b.tscn", scenes[1]);
    }

    [Fact]
    public void BuildScenesActive_includes_scene_save_path()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":1,\"type\":\"scene_save\",\"path\":\"res://x.tscn\"}",
                out var save));

        var scenes = ClipSceneExtractor.BuildScenesActive([save!]);

        Assert.Single(scenes);
        Assert.Equal("res://x.tscn", scenes[0]);
    }

    [Fact]
    public void BuildScenesActive_includes_playtest_start_scene_path()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":1,\"type\":\"playtest_start\",\"scene_path\":\"res://main.tscn\"}",
                out var start));

        var scenes = ClipSceneExtractor.BuildScenesActive([start!]);

        Assert.Single(scenes);
        Assert.Equal("res://main.tscn", scenes[0]);
    }

    [Fact]
    public void BuildScenesActive_events_before_snapshot_fallback()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":1,\"type\":\"playtest_start\",\"scene_path\":\"res://main.tscn\"}",
                out var start));

        var editorPaths = new EditorScenePaths("res://arena.tscn", null);
        var scenes = ClipSceneExtractor.BuildScenesActive([start!], editorPaths);

        Assert.Equal(2, scenes.Count);
        Assert.Equal("res://main.tscn", scenes[0]);
        Assert.Equal("res://arena.tscn", scenes[1]);
    }

    [Fact]
    public void BuildScenesActive_snapshot_playing_fallback_when_no_play_events()
    {
        var editorPaths = new EditorScenePaths(null, "res://play.tscn");
        var scenes = ClipSceneExtractor.BuildScenesActive([], editorPaths);

        Assert.Single(scenes);
        Assert.Equal("res://play.tscn", scenes[0]);
    }
}
