using Mnemonic;
using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class ScratchCapEnforcerTests
{
    [Fact]
    public void Enforce_empty_dir()
    {
        var dir = CreateScratchDir();
        try
        {
            var result = ScratchCapEnforcer.Enforce(dir, MnemonicConstants.ScratchCapMinBytes);
            Assert.Equal(0, result.EvictedCount);
            Assert.Equal(0, result.TotalBytes);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_under_cap()
    {
        var dir = CreateScratchDir();
        try
        {
            var t0 = DateTime.UtcNow.AddHours(-2);
            var t1 = t0.AddMinutes(1);
            WriteSegment(dir, "a_segment_00000.mp4", 100, t0);
            WriteSegment(dir, "b_segment_00001.mp4", 100, t1);

            var result = ScratchCapEnforcer.Enforce(dir, 1000);
            Assert.Equal(0, result.EvictedCount);
            Assert.Equal(200, result.TotalBytes);
            Assert.True(File.Exists(Path.Combine(dir, "a_segment_00000.mp4")));
            Assert.True(File.Exists(Path.Combine(dir, "b_segment_00001.mp4")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_evicts_oldest_until_under_cap()
    {
        var dir = CreateScratchDir();
        try
        {
            var t0 = DateTime.UtcNow.AddHours(-3);
            WriteSegment(dir, "a_segment_00000.mp4", 400, t0);
            WriteSegment(dir, "b_segment_00001.mp4", 400, t0.AddMinutes(1));
            WriteSegment(dir, "c_segment_00002.mp4", 400, t0.AddMinutes(2));

            var result = ScratchCapEnforcer.Enforce(dir, 1000);
            Assert.Equal(1, result.EvictedCount);
            Assert.Equal(800, result.TotalBytes);
            Assert.False(File.Exists(Path.Combine(dir, "a_segment_00000.mp4")));
            Assert.True(File.Exists(Path.Combine(dir, "b_segment_00001.mp4")));
            Assert.True(File.Exists(Path.Combine(dir, "c_segment_00002.mp4")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_evicts_multiple()
    {
        var dir = CreateScratchDir();
        try
        {
            var t0 = DateTime.UtcNow.AddHours(-5);
            for (var i = 0; i < 5; i++)
            {
                WriteSegment(dir, $"s_segment_{i:D5}.mp4", 100, t0.AddMinutes(i));
            }

            var result = ScratchCapEnforcer.Enforce(dir, 250);
            Assert.Equal(3, result.EvictedCount);
            Assert.Equal(200, result.TotalBytes);
            Assert.False(File.Exists(Path.Combine(dir, "s_segment_00000.mp4")));
            Assert.False(File.Exists(Path.Combine(dir, "s_segment_00001.mp4")));
            Assert.False(File.Exists(Path.Combine(dir, "s_segment_00002.mp4")));
            Assert.True(File.Exists(Path.Combine(dir, "s_segment_00003.mp4")));
            Assert.True(File.Exists(Path.Combine(dir, "s_segment_00004.mp4")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_grace_blocks_eviction()
    {
        var dir = CreateScratchDir();
        try
        {
            var oldest = DateTime.UtcNow.AddSeconds(-2);
            var newer = oldest.AddSeconds(1);
            WriteSegment(dir, "a_segment_00000.mp4", 600, oldest);
            WriteSegment(dir, "b_segment_00001.mp4", 600, newer);

            var result = ScratchCapEnforcer.Enforce(dir, 500);
            Assert.Equal(0, result.EvictedCount);
            Assert.True(File.Exists(Path.Combine(dir, "a_segment_00000.mp4")));
            Assert.True(File.Exists(Path.Combine(dir, "b_segment_00001.mp4")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Enforce_single_file_over_cap_not_deleted()
    {
        var dir = CreateScratchDir();
        try
        {
            WriteSegment(dir, "only_segment_00000.mp4", 2000, DateTime.UtcNow.AddHours(-1));

            var result = ScratchCapEnforcer.Enforce(dir, 500);
            Assert.Equal(0, result.EvictedCount);
            Assert.True(File.Exists(Path.Combine(dir, "only_segment_00000.mp4")));
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
            WriteSegment(dir, "random.mp4", 10_000_000, DateTime.UtcNow.AddHours(-2));
            WriteSegment(dir, "p_segment_00000.mp4", 100, DateTime.UtcNow.AddHours(-1));

            var result = ScratchCapEnforcer.Enforce(dir, 200);
            Assert.Equal(0, result.EvictedCount);
            Assert.True(File.Exists(Path.Combine(dir, "random.mp4")));
            Assert.True(File.Exists(Path.Combine(dir, "p_segment_00000.mp4")));
            Assert.Equal(100, result.TotalBytes);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateScratchDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_scratch_{Guid.NewGuid():N}");
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
