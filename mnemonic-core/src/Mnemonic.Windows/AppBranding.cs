using System.Diagnostics;
using System.Drawing;
namespace Mnemonic.Windows;

internal static class AppBranding
{
    private static readonly object Gate = new();
    private static Icon? _appIcon;
    private static bool _loadAttempted;

    public static Icon AppIcon
    {
        get
        {
            EnsureLoaded();
            return _appIcon ?? SystemIcons.Application;
        }
    }

    public static Icon CreateAppIconCopy()
    {
        EnsureLoaded();
        if (_appIcon is null)
        {
            return (Icon)SystemIcons.Application.Clone();
        }

        return (Icon)_appIcon.Clone();
    }

    /// <summary>Tray icon (same as app icon; recording state is shown in the status menu).</summary>
    public static Icon CreateTrayIcon(bool recording) => CreateAppIconCopy();

    private static void EnsureLoaded()
    {
        if (_loadAttempted)
        {
            return;
        }

        lock (Gate)
        {
            if (_loadAttempted)
            {
                return;
            }

            _loadAttempted = true;
            var path = Path.Combine(AppContext.BaseDirectory, "assets", "mnemonic.ico");
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                _appIcon = new Icon(path);
            }
            catch (Exception ex) when (ex is ArgumentException or System.ComponentModel.Win32Exception)
            {
                Trace.TraceWarning(
                    "AppBranding: could not load icon at {0}: {1}",
                    path,
                    ex.Message);
            }
        }
    }
}
