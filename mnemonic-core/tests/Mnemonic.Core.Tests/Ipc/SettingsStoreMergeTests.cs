using System.Text.Json;
using Mnemonic.Heuristic;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class SettingsStoreMergeTests
{
    [Fact]
    public void Save_does_not_clobber_hook_heuristics_when_core_in_memory_heuristics_null()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            // Core startup: loaded settings without heuristics in memory.
            var coreInMemory = new AppSettings
            {
                PreserveThreshold = 10,
                DrawMouse = true,
                SegmentDurationSeconds = 120,
            };
            store.Save(coreInMemory);

            // Hook writes heuristics via merge (simulated on disk).
            SimulateHookHeuristicsWrite(
                paths.SettingsFile,
                new Dictionary<string, HeuristicTypeSettings>(StringComparer.Ordinal)
                {
                    ["git_commit"] = new() { Enabled = false, Weight = 0, Cap = 0 },
                });

            // Core tray / bootstrap saves stale in-memory object (Heuristics still null).
            store.Save(coreInMemory);

            var onDisk = store.Load();
            Assert.NotNull(onDisk.Heuristics);
            Assert.True(onDisk.Heuristics!.TryGetValue("git_commit", out var gitCommit));
            Assert.False(gitCommit!.Enabled);
            Assert.Equal(0, gitCommit.Weight);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Save_does_not_clobber_hook_heuristics_when_core_in_memory_heuristics_stale()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            var coreInMemory = new AppSettings
            {
                PreserveThreshold = 10,
                Heuristics = new Dictionary<string, HeuristicTypeSettings>(StringComparer.Ordinal)
                {
                    ["git_commit"] = new() { Enabled = true, Weight = 9, Cap = 0 },
                },
            };
            store.Save(coreInMemory);

            SimulateHookHeuristicsWrite(
                paths.SettingsFile,
                new Dictionary<string, HeuristicTypeSettings>(StringComparer.Ordinal)
                {
                    ["git_commit"] = new() { Enabled = false, Weight = 0, Cap = 0 },
                });

            store.Save(coreInMemory);

            var onDisk = store.Load();
            Assert.NotNull(onDisk.Heuristics);
            Assert.True(onDisk.Heuristics!.TryGetValue("git_commit", out var gitCommit));
            Assert.False(gitCommit!.Enabled);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Save_does_not_clobber_hook_preserve_threshold_when_core_in_memory_stale()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            var coreInMemory = new AppSettings { PreserveThreshold = 10 };
            store.Save(coreInMemory);

            SimulateHookIntWrite(paths.SettingsFile, "preserve_threshold", 15);

            store.Save(coreInMemory);

            Assert.Equal(15, store.Load().PreserveThreshold);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Save_does_not_clobber_hook_highlight_score_min_when_core_in_memory_stale()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            var coreInMemory = new AppSettings { HighlightScoreMin = 20 };
            store.Save(coreInMemory);

            SimulateHookIntWrite(paths.SettingsFile, "highlight_score_min", 25);

            store.Save(coreInMemory);

            Assert.Equal(25, store.Load().HighlightScoreMin);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Save_does_not_clobber_hook_segment_history_max_entries_when_core_in_memory_stale()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            var coreInMemory = new AppSettings { SegmentHistoryMaxEntries = 200 };
            store.Save(coreInMemory);

            SimulateHookIntWrite(paths.SettingsFile, "segment_history_max_entries", 500);

            store.Save(coreInMemory);

            Assert.Equal(500, store.Load().SegmentHistoryMaxEntries);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SaveFromTray_persists_draw_mouse_when_disk_has_opposite_value()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            store.Save(new AppSettings { DrawMouse = true });

            var trayIncoming = store.Load();
            trayIncoming.DrawMouse = false;
            store.SaveFromTray(trayIncoming);

            Assert.False(store.Load().DrawMouse);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SaveFromTray_persists_highlight_score_min_when_disk_has_opposite_value()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            store.Save(new AppSettings { HighlightScoreMin = 25 });

            var trayIncoming = store.Load();
            trayIncoming.HighlightScoreMin = 20;
            store.SaveFromTray(trayIncoming);

            Assert.Equal(20, store.Load().HighlightScoreMin);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SaveFromTray_persists_preserve_threshold_when_disk_has_opposite_value()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            store.Save(new AppSettings { PreserveThreshold = 15 });

            var trayIncoming = store.Load();
            trayIncoming.PreserveThreshold = 12;
            store.SaveFromTray(trayIncoming);

            Assert.Equal(12, store.Load().PreserveThreshold);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SaveFromTray_persists_notable_score_min_when_disk_has_opposite_value()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            store.Save(new AppSettings { NotableScoreMin = 20 });

            var trayIncoming = store.Load();
            trayIncoming.NotableScoreMin = 14;
            store.SaveFromTray(trayIncoming);

            Assert.Equal(14, store.Load().NotableScoreMin);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Save_still_applies_core_capture_field_changes()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);

            var initial = new AppSettings
            {
                PreserveThreshold = 10,
                Heuristics = new Dictionary<string, HeuristicTypeSettings>(StringComparer.Ordinal)
                {
                    ["git_commit"] = new() { Enabled = false, Weight = 0, Cap = 0 },
                },
            };
            store.Save(initial);

            var coreInMemory = store.Load();
            coreInMemory.DrawMouse = false;
            coreInMemory.MicDeviceId = "{0.0.1.00000000}.{test}";
            coreInMemory.CaptureMicEnabled = true;
            store.Save(coreInMemory);

            var onDisk = store.Load();
            Assert.False(onDisk.DrawMouse);
            Assert.Equal("{0.0.1.00000000}.{test}", onDisk.MicDeviceId);
            Assert.True(onDisk.Heuristics!.TryGetValue("git_commit", out var gitCommit));
            Assert.False(gitCommit!.Enabled);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void SimulateHookHeuristicsWrite(
        string settingsFile,
        Dictionary<string, HeuristicTypeSettings> heuristics)
    {
        var json = File.ReadAllText(settingsFile);
        using var doc = JsonDocument.Parse(json);
        var dic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        dic["heuristics"] = JsonSerializer.SerializeToElement(
            heuristics,
            JsonOptions.Shared);
        File.WriteAllText(settingsFile, JsonSerializer.Serialize(dic, JsonOptions.Shared));
    }

    private static void SimulateHookIntWrite(string settingsFile, string key, int value)
    {
        var json = File.ReadAllText(settingsFile);
        var dic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        dic[key] = JsonSerializer.SerializeToElement(value, JsonOptions.Shared);
        File.WriteAllText(settingsFile, JsonSerializer.Serialize(dic, JsonOptions.Shared));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "mnemonic-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
