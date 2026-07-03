using Mnemonic.Capture;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class CaptureServicePauseTests
{
    [Fact]
    public void Pause_without_active_process_writes_paused_status()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var statusStore = new StatusStore(paths);
            var settings = new AppSettings();
            var ffmpeg = new FfmpegResolution(null, false, "");
            var closedCount = 0;

            using var capture = new CaptureService(paths, statusStore, settings, ffmpeg);
            capture.SegmentClosed += _ => closedCount++;

            capture.Pause();

            var snapshot = statusStore.Read();
            Assert.NotNull(snapshot);
            Assert.Equal(CaptureStates.Paused, snapshot!.State);
            Assert.False(snapshot.Recording);
            Assert.Equal(0, closedCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_capture_pause_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
