using Mnemonic.Capture;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;

namespace Mnemonic;

public static class CoreBootstrap
{
    public sealed record Result(DataRootPaths Paths, AppSettings Settings, FfmpegResolution Ffmpeg);

    public static Result Initialize()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Mnemonic Core v1 supports Windows only.");
        }

        var paths = new DataRootPaths();
        new DataRootLayout().EnsureExists(paths);

        var settingsStore = new SettingsStore(paths);
        var settings = settingsStore.Load();
        if (SettingsSanitizer.Sanitize(settings))
        {
            settingsStore.Save(settings);
        }

        var ffmpeg = FfmpegLocator.Resolve(settings, AppContext.BaseDirectory);
        FfmpegProcessCleanup.KillBundledOrphans(ffmpeg.ExecutablePath);

        var statusStore = new StatusStore(paths);
        statusStore.WriteIdle(ffmpeg.IsAvailable, ffmpeg.ErrorMessage);

        return new Result(paths, settings, ffmpeg);
    }
}
