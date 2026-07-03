using System.Diagnostics;

namespace Mnemonic.Retention;

public static class ClipThumbnailGenerator
{
    private static readonly TimeSpan GenerateTimeout = TimeSpan.FromSeconds(10);

    public static bool TryGenerateWithRetry(
        string ffmpegExecutablePath,
        string videoPath,
        string thumbOutputPath,
        int widthPx,
        int heightPx,
        int maxAttempts = 4,
        int retryDelayMs = 150)
    {
        if (maxAttempts < 1)
        {
            maxAttempts = 1;
        }

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (attempt > 0)
            {
                Thread.Sleep(retryDelayMs);
            }

            if (TryGenerate(ffmpegExecutablePath, videoPath, thumbOutputPath, widthPx, heightPx))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGenerate(
        string ffmpegExecutablePath,
        string videoPath,
        string thumbOutputPath,
        int widthPx,
        int heightPx)
    {
        if (string.IsNullOrWhiteSpace(ffmpegExecutablePath)
            || string.IsNullOrWhiteSpace(videoPath)
            || string.IsNullOrWhiteSpace(thumbOutputPath))
        {
            return false;
        }

        if (!File.Exists(videoPath))
        {
            return false;
        }

        var parentDir = Path.GetDirectoryName(thumbOutputPath);
        if (!string.IsNullOrEmpty(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        var vf = $"scale={widthPx}:{heightPx}:force_original_aspect_ratio=decrease,"
            + $"pad={widthPx}:{heightPx}:(ow-iw)/2:(oh-ih)/2";
        // First decodable frame only: -ss before -i fails on short fMP4 segments (<~1s).
        var arguments =
            $"-hide_banner -loglevel error -y -i \"{videoPath}\" -frames:v 1 "
            + $"-vf \"{vf}\" -q:v 5 \"{thumbOutputPath}\"";

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = ffmpegExecutablePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            if (!process.Start())
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        try
        {
            if (!process.WaitForExit(GenerateTimeout))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort.
                }

                return false;
            }
        }
        catch
        {
            return false;
        }

        if (process.ExitCode != 0)
        {
            return false;
        }

        return File.Exists(thumbOutputPath) && new FileInfo(thumbOutputPath).Length > 0;
    }
}
