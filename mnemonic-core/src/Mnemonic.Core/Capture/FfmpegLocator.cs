using Mnemonic.Ipc.Models;

namespace Mnemonic.Capture;

public static class FfmpegLocator
{
    public static FfmpegResolution Resolve(AppSettings settings, string applicationBaseDirectory)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Mnemonic Core v1 supports Windows only.");
        }

        var overridePath = settings.FfmpegPathOverride?.Trim() ?? "";

        string candidate;
        if (overridePath.Length > 0)
        {
            if (!File.Exists(overridePath))
            {
                return new FfmpegResolution(
                    null,
                    false,
                    $"FFmpeg override path not found: {overridePath}");
            }

            candidate = overridePath;
        }
        else
        {
            candidate = FfmpegBundlePaths.GetBundledExecutablePath(applicationBaseDirectory);
            if (!File.Exists(candidate))
            {
                return new FfmpegResolution(
                    null,
                    false,
                    $"Bundled FFmpeg not found at {candidate}");
            }
        }

        if (!FfmpegProbe.TryVerifyCaptureCapabilities(candidate, out var probeError))
        {
            return new FfmpegResolution(
                null,
                false,
                $"FFmpeg failed capture capability check at {candidate}: {probeError}");
        }

        return new FfmpegResolution(candidate, true, "");
    }
}
