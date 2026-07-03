using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class EditorHeuristicDeriverTests
{
    [Fact]
    public void Save_burst_on_third_distinct_path()
    {
        var deriver = new EditorHeuristicDeriver();
        var all = new List<SessionEvent>();
        foreach (var line in new[]
                 {
                     "{\"t\":100,\"type\":\"scene_save\",\"path\":\"res://a.tscn\"}",
                     "{\"t\":110,\"type\":\"scene_save\",\"path\":\"res://b.tscn\"}",
                     "{\"t\":120,\"type\":\"scene_save\",\"path\":\"res://c.tscn\"}",
                 })
        {
            Assert.True(SessionEvent.TryParseFromJsonLine(line, out var raw));
            all.AddRange(deriver.ProcessDerived(raw!));
        }

        Assert.Single(all, e => e.Type == "save_burst" && e.T == 120);
    }

    [Fact]
    public void Save_burst_no_emit_on_two_paths()
    {
        var deriver = new EditorHeuristicDeriver();
        var all = new List<SessionEvent>();
        foreach (var line in new[]
                 {
                     "{\"t\":100,\"type\":\"scene_save\",\"path\":\"res://a.tscn\"}",
                     "{\"t\":110,\"type\":\"scene_save\",\"path\":\"res://b.tscn\"}",
                 })
        {
            Assert.True(SessionEvent.TryParseFromJsonLine(line, out var raw));
            all.AddRange(deriver.ProcessDerived(raw!));
        }

        Assert.DoesNotContain(all, e => e.Type == "save_burst");
    }

    [Fact]
    public void Iteration_cycle_after_save_then_playtest()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":100,\"type\":\"scene_save\",\"path\":\"res://a.tscn\"}",
                out var save));
        Assert.True(
            SessionEvent.TryParseFromJsonLine("{\"t\":150,\"type\":\"playtest_start\"}", out var start));

        var deriver = new EditorHeuristicDeriver();
        deriver.ProcessDerived(save!).ToList();
        var derived = deriver.ProcessDerived(start!).ToList();

        Assert.Single(derived, e => e.Type == "iteration_cycle" && e.T == 150);
    }

    [Fact]
    public void No_iteration_cycle_when_gap_too_large()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":100,\"type\":\"scene_save\",\"path\":\"res://a.tscn\"}",
                out var save));
        Assert.True(
            SessionEvent.TryParseFromJsonLine("{\"t\":300,\"type\":\"playtest_start\"}", out var start));

        var deriver = new EditorHeuristicDeriver();
        deriver.ProcessDerived(save!).ToList();
        var derived = deriver.ProcessDerived(start!).ToList();

        Assert.DoesNotContain(derived, e => e.Type == "iteration_cycle");
    }

    [Fact]
    public void Commit_after_playtest_within_window()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":200,\"type\":\"playtest_stop\",\"duration_sec\":10}",
                out var stop));
        Assert.True(
            SessionEvent.TryParseFromJsonLine("{\"t\":400,\"type\":\"git_commit\"}", out var commit));

        var deriver = new EditorHeuristicDeriver();
        deriver.ProcessDerived(stop!).ToList();
        var derived = deriver.ProcessDerived(commit!).ToList();

        Assert.Single(derived, e => e.Type == "commit_after_playtest" && e.T == 400);
    }

    [Fact]
    public void No_commit_after_playtest_when_gap_exceeded()
    {
        Assert.True(
            SessionEvent.TryParseFromJsonLine(
                "{\"t\":100,\"type\":\"playtest_stop\",\"duration_sec\":10}",
                out var stop));
        Assert.True(
            SessionEvent.TryParseFromJsonLine("{\"t\":800,\"type\":\"git_commit\"}", out var commit));

        var deriver = new EditorHeuristicDeriver();
        deriver.ProcessDerived(stop!).ToList();
        var derived = deriver.ProcessDerived(commit!).ToList();

        Assert.DoesNotContain(derived, e => e.Type == "commit_after_playtest");
    }

    [Fact]
    public void Resource_burst_on_third_distinct_path()
    {
        var deriver = new EditorHeuristicDeriver();
        var all = new List<SessionEvent>();
        foreach (var line in new[]
                 {
                     "{\"t\":100,\"type\":\"resource_saved\",\"path\":\"res://a.gd\"}",
                     "{\"t\":110,\"type\":\"resource_saved\",\"path\":\"res://b.gd\"}",
                     "{\"t\":120,\"type\":\"resource_saved\",\"path\":\"res://c.gd\"}",
                 })
        {
            Assert.True(SessionEvent.TryParseFromJsonLine(line, out var raw));
            all.AddRange(deriver.ProcessDerived(raw!));
        }

        Assert.Single(all, e => e.Type == "resource_burst" && e.T == 120);
    }
}
