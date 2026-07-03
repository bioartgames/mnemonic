using System.Diagnostics;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ClipThumbnailGeneratorTests
{
    [Fact]
    public void TryGenerate_EmptyFfmpegPath_ReturnsFalse()
    {
        var root = CreateTempDir();
        try
        {
            var video = Path.Combine(root, "video.mp4");
            File.WriteAllBytes(video, [0, 1, 2, 3]);
            var thumb = Path.Combine(root, "thumb.jpg");

            Assert.False(ClipThumbnailGenerator.TryGenerate("", video, thumb, 96, 54));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryGenerate_MissingVideo_ReturnsFalse()
    {
        var root = CreateTempDir();
        try
        {
            var thumb = Path.Combine(root, "thumb.jpg");
            Assert.False(ClipThumbnailGenerator.TryGenerate("ffmpeg.exe", Path.Combine(root, "nope.mp4"), thumb, 96, 54));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryGenerate_InvalidVideoBytes_ReturnsFalse()
    {
        var root = CreateTempDir();
        try
        {
            var video = Path.Combine(root, "video.mp4");
            File.WriteAllBytes(video, [0, 1, 2, 3]);
            var thumb = Path.Combine(root, "thumb.jpg");

            Assert.False(ClipThumbnailGenerator.TryGenerate("ffmpeg.exe", video, thumb, 96, 54));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryGenerateWithRetry_InvalidVideoBytes_ReturnsFalse()
    {
        var root = CreateTempDir();
        try
        {
            var video = Path.Combine(root, "video.mp4");
            File.WriteAllBytes(video, [0, 1, 2, 3]);
            var thumb = Path.Combine(root, "thumb.jpg");

            Assert.False(ClipThumbnailGenerator.TryGenerateWithRetry("ffmpeg.exe", video, thumb, 96, 54, 3, 1));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryGenerate_ShortFragmentedMp4_Succeeds()
    {
        var ffmpeg = TryResolveBundledFfmpeg();
        if (ffmpeg is null)
        {
            return;
        }

        var root = CreateTempDir();
        try
        {
            var video = CreateCaptureStyleSegmentMp4(ffmpeg, root, durationSeconds: 0.8);
            var thumb = Path.Combine(root, "thumb.jpg");

            Assert.True(ClipThumbnailGenerator.TryGenerate(ffmpeg, video, thumb, 96, 54));
            Assert.True(File.Exists(thumb));
            Assert.True(new FileInfo(thumb).Length > 0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateCaptureStyleSegmentMp4(string ffmpeg, string outputDir, double durationSeconds)
    {
        var pattern = Path.Combine(outputDir, "mn_test_segment_%05d.mp4");
        var d = durationSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        var args =
            $"-hide_banner -loglevel error -y -f lavfi -i color=c=blue:s=320x240:d={d} "
            + "-c:v libx264 -preset ultrafast -crf 28 -pix_fmt yuv420p -r 30 "
            + "-f segment -segment_time 120 -reset_timestamps 1 "
            + "-segment_format_options movflags=frag_keyframe+empty_moov+default_base_moof "
            + $"\"{pattern}\"";

        RunFfmpegOrThrow(ffmpeg, args);

        var video = Directory.EnumerateFiles(outputDir, "*.mp4").FirstOrDefault()
            ?? throw new InvalidOperationException("segment mp4 not created");

        return video;
    }

    private static void RunFfmpegOrThrow(string ffmpeg, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("failed to start ffmpeg");

        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("ffmpeg timed out");
        }

        if (process.ExitCode != 0)
        {
            var err = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"ffmpeg failed ({process.ExitCode}): {err}");
        }
    }

    private static string? TryResolveBundledFfmpeg()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "third_party", "ffmpeg", "win-x64", "bin", "ffmpeg.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(dir);
            if (parent is null)
            {
                break;
            }

            dir = parent.FullName;
        }

        return null;
    }

    private static string CreateTempDir()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_thumb_gen_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }
}
