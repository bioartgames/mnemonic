using Mnemonic.Heuristic;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class SettingsStoreHookFieldsReloadTests
{
    [Fact]
    public void TryMergeHookOwnedFieldsFromDisk_updates_heuristics_after_hook_write()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);
            var coreSettings = new AppSettings { PreserveThreshold = 10 };
            store.Save(coreSettings);

            SimulateHookHeuristicsWrite(
                paths.SettingsFile,
                new Dictionary<string, HeuristicTypeSettings>(StringComparer.Ordinal)
                {
                    ["git_commit"] = new() { Enabled = false, Weight = 0, Cap = 0 },
                });

            store.TryMergeHookOwnedFieldsFromDisk(coreSettings);

            Assert.NotNull(coreSettings.Heuristics);
            Assert.True(coreSettings.Heuristics!.TryGetValue("git_commit", out var gitCommit));
            Assert.False(gitCommit!.Enabled);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryMergeHookOwnedFieldsFromDisk_accepts_godot_float_fields()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new SettingsStore(paths);
            var coreSettings = new AppSettings { PreserveThreshold = 10, ScratchCapGb = 8 };
            store.Save(coreSettings);

            File.WriteAllText(
                paths.SettingsFile,
                File.ReadAllText(paths.SettingsFile).Replace(
                    "\"preserve_threshold\": 10",
                    "\"preserve_threshold\": 10,\n  \"heuristics\": {\"git_commit\": {\"enabled\": false, \"weight\": 0, \"cap\": 0}}",
                    StringComparison.Ordinal)
                    .Replace("\"scratch_cap_gb\": 8", "\"scratch_cap_gb\": 8.0", StringComparison.Ordinal));

            store.TryMergeHookOwnedFieldsFromDisk(coreSettings);

            Assert.NotNull(coreSettings.Heuristics);
            Assert.False(coreSettings.Heuristics!["git_commit"].Enabled);
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
        var dic = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json)!;
        dic["heuristics"] = System.Text.Json.JsonSerializer.SerializeToElement(
            heuristics,
            JsonOptions.Shared);
        File.WriteAllText(settingsFile, System.Text.Json.JsonSerializer.Serialize(dic, JsonOptions.Shared));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "mnemonic-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
