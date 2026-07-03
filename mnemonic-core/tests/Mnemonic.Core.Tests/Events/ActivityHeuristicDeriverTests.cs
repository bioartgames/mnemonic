using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class ActivityHeuristicDeriverTests
{
    [Fact]
    public void Edit_intensity_from_busy_packet()
    {
        var packet = SessionEvent.Create(
            200,
            "activity_packet",
            new Dictionary<string, object?>
            {
                ["window_sec"] = 60,
                ["scene_saves"] = 2,
                ["resource_saves"] = 2,
                ["scene_transitions"] = 0,
                ["playtest_active_sec"] = 0,
            });

        var deriver = new ActivityHeuristicDeriver();
        var derived = deriver.Process(packet).ToList();
        Assert.Single(derived, e => e.Type == "edit_intensity");
    }

    [Fact]
    public void Scene_hopping_without_playtest()
    {
        var packet = SessionEvent.Create(
            200,
            "activity_packet",
            new Dictionary<string, object?>
            {
                ["window_sec"] = 60,
                ["scene_saves"] = 0,
                ["resource_saves"] = 0,
                ["scene_transitions"] = 3,
                ["playtest_active_sec"] = 0,
            });

        var deriver = new ActivityHeuristicDeriver();
        var derived = deriver.Process(packet).ToList();
        Assert.Single(derived, e => e.Type == "scene_hopping");
    }

    [Fact]
    public void Checkpoint_after_work_follows_edit_intensity()
    {
        var deriver = new ActivityHeuristicDeriver();
        var packet = SessionEvent.Create(
            100,
            "activity_packet",
            new Dictionary<string, object?>
            {
                ["window_sec"] = 60,
                ["scene_saves"] = 2,
                ["resource_saves"] = 2,
                ["scene_transitions"] = 0,
                ["playtest_active_sec"] = 0,
            });
        deriver.Process(packet).ToList();

        var commit = SessionEvent.Create(400, "git_commit");
        var derived = deriver.Process(commit).ToList();
        Assert.Single(derived, e => e.Type == "checkpoint_after_work");
    }

    [Fact]
    public void Edit_intensity_scales_for_30s_window()
    {
        var packet = SessionEvent.Create(
            130,
            "activity_packet",
            new Dictionary<string, object?>
            {
                ["window_sec"] = 30,
                ["scene_saves"] = 2,
                ["resource_saves"] = 2,
                ["scene_transitions"] = 0,
                ["playtest_active_sec"] = 0,
            });

        var deriver = new ActivityHeuristicDeriver();
        var derived = deriver.Process(packet).ToList();
        Assert.Single(derived, e => e.Type == "edit_intensity");
    }

    [Fact]
    public void Script_focus_fires_on_script_dominance()
    {
        var packet = SessionEvent.Create(
            160,
            "activity_packet",
            new Dictionary<string, object?>
            {
                ["window_sec"] = 60,
                ["scene_saves"] = 0,
                ["resource_saves"] = 2,
                ["scene_transitions"] = 0,
                ["playtest_active_sec"] = 0,
                ["focus_script_sec"] = 30,
            });

        var deriver = new ActivityHeuristicDeriver();
        var derived = deriver.Process(packet).ToList();
        Assert.Single(derived, e => e.Type == "script_focus");
    }

    [Fact]
    public void Layout_focus_fires_on_2d_dominance()
    {
        var packet = SessionEvent.Create(
            160,
            "activity_packet",
            new Dictionary<string, object?>
            {
                ["window_sec"] = 60,
                ["scene_saves"] = 0,
                ["resource_saves"] = 0,
                ["scene_transitions"] = 2,
                ["playtest_active_sec"] = 0,
                ["focus_2d_sec"] = 30,
                ["focus_3d_sec"] = 0,
            });

        var deriver = new ActivityHeuristicDeriver();
        var derived = deriver.Process(packet).ToList();
        Assert.Single(derived, e => e.Type == "layout_focus");
    }

    [Fact]
    public void Pattern_detail_attached()
    {
        var packet = SessionEvent.Create(
            200,
            "activity_packet",
            new Dictionary<string, object?>
            {
                ["window_sec"] = 60,
                ["scene_saves"] = 2,
                ["resource_saves"] = 2,
                ["scene_transitions"] = 0,
                ["playtest_active_sec"] = 0,
            });

        var deriver = new ActivityHeuristicDeriver();
        var derived = deriver.Process(packet).Single(e => e.Type == "edit_intensity");
        Assert.Contains("2 resource", SessionEventExtras.GetString(derived, "pattern_detail") ?? "");
    }
}
