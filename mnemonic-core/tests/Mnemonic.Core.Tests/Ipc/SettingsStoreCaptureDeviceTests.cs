using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class SettingsStoreCaptureDeviceTests
{
    [Fact]
    public void SaveLoad_round_trips_realtek_style_device_ids()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.ControlDir);
            var store = new SettingsStore(paths);
            var expected = new AppSettings
            {
                CaptureMicEnabled = true,
                MicDeviceId = "{0.0.1.00000000}.{11111111-1111-1111-1111-111111111111}",
                CaptureDesktopEnabled = true,
                DesktopLoopbackDeviceId = "{0.0.0.00000000}.{33333333-3333-3333-3333-333333333333}",
                DrawMouse = true,
                SegmentDurationSeconds = 120,
            };

            store.Save(expected);
            var loaded = store.Load();

            Assert.Equal(expected.MicDeviceId, loaded.MicDeviceId);
            Assert.Equal(expected.DesktopLoopbackDeviceId, loaded.DesktopLoopbackDeviceId);
            Assert.True(loaded.CaptureMicEnabled);
            Assert.True(loaded.CaptureDesktopEnabled);
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
