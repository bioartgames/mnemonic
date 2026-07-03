using Mnemonic;
using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class ScratchStaleCleanupTests
{
    [Fact]
    public void Enforce_empty_dir()
    {
        var dir = CreateScratchDir();
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = ScratchStaleCleanup.Enforce(dir, recording: false, "p", 0, now);
            Assert.Equal(0, result.Scanned);
            Assert.Equal(0, result.Deleted);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_skips_recent_while_idle()
    {
        var dir = CreateScratchDir();
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            WriteSegment(dir, "a_segment_00000.mp4", 10, DateTime.UtcNow.AddMinutes(-5));

            var result = ScratchStaleCleanup.Enforce(dir, recording: false, null, -1, now);
            Assert.Equal(1, result.Scanned);
            Assert.Equal(0, result.Deleted);
            Assert.True(File.Exists(Path.Combine(dir, "a_segment_00000.mp4")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_deletes_old_while_idle()
    {
        var dir = CreateScratchDir();
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            WriteSegment(dir, "a_segment_00000.mp4", 10, DateTime.UtcNow.AddMinutes(-15));

            var result = ScratchStaleCleanup.Enforce(dir, recording: false, null, -1, now);
            Assert.Equal(1, result.Scanned);
            Assert.Equal(1, result.Deleted);
            Assert.False(File.Exists(Path.Combine(dir, "a_segment_00000.mp4")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_protects_active_prefix_recent_indices()
    {
        var dir = CreateScratchDir();
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var old = DateTime.UtcNow.AddMinutes(-35);
            WriteSegment(dir, "p_segment_00004.mp4", 10, old);
            WriteSegment(dir, "p_segment_00005.mp4", 10, old);

            var result = ScratchStaleCleanup.Enforce(dir, recording: true, "p", 5, now);
            Assert.Equal(2, result.Scanned);
            Assert.Equal(1, result.Deleted);
            Assert.Equal(1, result.SkippedProtected);
            Assert.False(File.Exists(Path.Combine(dir, "p_segment_00004.mp4")));
            Assert.True(File.Exists(Path.Combine(dir, "p_segment_00005.mp4")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_deletes_other_prefix_while_recording()
    {
        var dir = CreateScratchDir();
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            WriteSegment(dir, "b_segment_00001.mp4", 10, DateTime.UtcNow.AddMinutes(-35));

            var result = ScratchStaleCleanup.Enforce(dir, recording: true, "a", 5, now);
            Assert.Equal(1, result.Scanned);
            Assert.Equal(1, result.Deleted);
            Assert.False(File.Exists(Path.Combine(dir, "b_segment_00001.mp4")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_respects_max_deletes()
    {
        var dir = CreateScratchDir();
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var old = DateTime.UtcNow.AddMinutes(-35);
            for (var i = 0; i < 250; i++)
            {
                WriteSegment(dir, $"x_segment_{i:D5}.mp4", 1, old);
            }

            var result = ScratchStaleCleanup.Enforce(dir, recording: false, null, -1, now);
            Assert.Equal(MnemonicConstants.ScratchStaleCleanupMaxDeletesPerRun, result.Deleted);
            Assert.Equal(50, Directory.GetFiles(dir, "*.mp4").Length);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_ignores_non_segment_mp4()
    {
        var dir = CreateScratchDir();
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            WriteSegment(dir, "random.mp4", 10, DateTime.UtcNow.AddMinutes(-35));

            var result = ScratchStaleCleanup.Enforce(dir, recording: false, null, -1, now);
            Assert.Equal(0, result.Scanned);
            Assert.True(File.Exists(Path.Combine(dir, "random.mp4")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateScratchDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_scratch_stale_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void WriteSegment(string dir, string fileName, int sizeBytes, DateTime mtimeUtc)
    {
        var path = Path.Combine(dir, fileName);
        File.WriteAllBytes(path, new byte[sizeBytes]);
        File.SetLastWriteTimeUtc(path, mtimeUtc);
    }
}
