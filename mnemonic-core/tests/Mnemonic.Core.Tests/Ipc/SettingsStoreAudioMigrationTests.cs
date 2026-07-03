using System.Text.Json;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class SettingsStoreAudioMigrationTests
{
    [Fact]
    public void Load_migrates_legacy_silent_audio_defaults()
    {
        var root = Path.Combine(Path.GetTempPath(), "mnemonic-audio-migrate-" + Guid.NewGuid().ToString("N"));
        var paths = new DataRootPaths(root);
        Directory.CreateDirectory(paths.ControlDir);

        var legacy = new AppSettings
        {
            CaptureMicEnabled = false,
            CaptureDesktopEnabled = false,
            MicDeviceId = "",
            DesktopLoopbackDeviceId = "",
        };
        File.WriteAllText(
            paths.SettingsFile,
            JsonSerializer.Serialize(legacy, SettingsJsonOptions.Shared));

        var store = new SettingsStore(paths);
        var loaded = store.Load();

        Assert.True(loaded.CaptureMicEnabled);
        Assert.True(loaded.CaptureDesktopEnabled);

        var onDisk = JsonSerializer.Deserialize<AppSettings>(
            File.ReadAllText(paths.SettingsFile),
            SettingsJsonOptions.Shared);
        Assert.NotNull(onDisk);
        Assert.True(onDisk!.CaptureMicEnabled);
        Assert.True(onDisk.CaptureDesktopEnabled);

        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch
        {
            // Best-effort temp cleanup.
        }
    }
}
