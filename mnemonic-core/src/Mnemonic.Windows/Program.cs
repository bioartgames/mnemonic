using Mnemonic.Capture;

namespace Mnemonic.Windows;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var guard = new SingleInstanceGuard();
        if (!guard.IsPrimaryInstance)
        {
            MessageBox.Show(
                $"{MnemonicConstants.ProductName} is already running.",
                MnemonicConstants.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        CoreBootstrap.Result bootstrap;
        try
        {
            bootstrap = CoreBootstrap.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize DataRoot:\n{ex.Message}",
                MnemonicConstants.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var bundledFfmpegPath = bootstrap.Ffmpeg.ExecutablePath;
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            FfmpegProcessCleanup.KillBundledOrphans(bundledFfmpegPath);

        Application.Run(new HiddenHostForm(bootstrap));
    }
}
