using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class SettingsStoreGodotFloatTests
{
    [Fact]
    public void Load_accepts_godot_style_float_integers_in_settings_json()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            File.WriteAllText(
                paths.SettingsFile,
                """
                {
                  "preserve_threshold": 10.0,
                  "highlight_score_min": 25.0,
                  "segment_duration_seconds": 120.0
                }
                """);

            var loaded = new SettingsStore(paths).Load();

            Assert.Equal(10, loaded.PreserveThreshold);
            Assert.Equal(25, loaded.HighlightScoreMin);
            Assert.Equal(120, loaded.SegmentDurationSeconds);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "mnemonic-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
