using Mnemonic.Capture;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class CaptureBootRecordingPolicyTests
{
    [Fact]
    public void When_start_recording_on_launch_false_writes_paused_status()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var statusStore = new StatusStore(paths);
            var settings = new AppSettings { StartRecordingOnLaunch = false };
            var ffmpeg = new FfmpegResolution(null, false, "");
            var closedCount = 0;

            using var capture = new CaptureService(paths, statusStore, settings, ffmpeg);
            capture.SegmentClosed += _ => closedCount++;

            ApplyBootRecordingPolicy(capture, settings);

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

    [Fact]
    public void When_start_recording_on_launch_true_does_not_write_paused_status()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var statusStore = new StatusStore(paths);
            var settings = new AppSettings { StartRecordingOnLaunch = true };
            var ffmpeg = new FfmpegResolution(null, false, "");

            using var capture = new CaptureService(paths, statusStore, settings, ffmpeg);

            ApplyBootRecordingPolicy(capture, settings);

            var snapshot = statusStore.Read();
            Assert.True(snapshot is null || snapshot.State != CaptureStates.Paused);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void ApplyBootRecordingPolicy(CaptureService capture, AppSettings settings)
    {
        if (settings.StartRecordingOnLaunch)
        {
            capture.Start();
        }
        else
        {
            capture.Pause();
        }
    }

    private static string CreateTempRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_capture_boot_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
