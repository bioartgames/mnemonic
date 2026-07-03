using System.Diagnostics;
using Mnemonic.Capture;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;

namespace Mnemonic.Windows.Tray;

internal sealed class TrayController : IDisposable
{
    private const int MaxErrorMenuLength = 120;

    private readonly DataRootPaths _paths;
    private readonly StatusStore _statusStore;
    private readonly SettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private readonly FfmpegResolution _ffmpeg;
    private readonly Form _ownerForm;

    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _baseTrayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _resumeItem;
    private readonly System.Windows.Forms.Timer _statusPollTimer;

    public TrayController(
        DataRootPaths paths,
        StatusStore statusStore,
        SettingsStore settingsStore,
        AppSettings settings,
        FfmpegResolution ffmpeg,
        Form ownerForm)
    {
        _paths = paths;
        _statusStore = statusStore;
        _settingsStore = settingsStore;
        _settings = settings;
        _ffmpeg = ffmpeg;
        _ownerForm = ownerForm;

        _statusItem = new ToolStripMenuItem { Enabled = false };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open Mnemonic data", null, (_, _) => OpenDataRoot());
        menu.Items.Add("Settings…", null, (_, _) => ShowSettings());
        menu.Items.Add("Segment log…", null, (_, _) => ShowSegmentLog());
        menu.Items.Add(new ToolStripSeparator());
        _pauseItem = new ToolStripMenuItem(TrayUi.StopRecording, null, (_, _) =>
        {
            if (_ownerForm is HiddenHostForm host)
            {
                host.StopRecordingSession();
            }
        });
        _pauseItem.ToolTipText = TrayUi.TooltipStopRecording;
        _resumeItem = new ToolStripMenuItem(TrayUi.StartRecording, null, (_, _) =>
        {
            if (_ownerForm is HiddenHostForm host)
            {
                host.StartRecordingSession();
            }
        });
        _resumeItem.ToolTipText = TrayUi.TooltipStartRecording;
        menu.Items.Add(_pauseItem);
        menu.Items.Add(_resumeItem);

        _baseTrayIcon = AppBranding.CreateAppIconCopy();
        _notifyIcon = new NotifyIcon
        {
            Icon = _baseTrayIcon,
            Text = MnemonicConstants.ProductName,
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.DoubleClick += (_, _) => OpenDataRoot();

        _statusPollTimer = new System.Windows.Forms.Timer
        {
            Interval = MnemonicConstants.TrayStatusPollIntervalMs,
        };
        _statusPollTimer.Tick += (_, _) => RefreshStatusMenu();
        _statusPollTimer.Start();

        RefreshStatusMenu();
    }

    public void Dispose()
    {
        _statusPollTimer.Stop();
        _statusPollTimer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _baseTrayIcon.Dispose();
    }

    private void OpenDataRoot()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _paths.Root,
            UseShellExecute = true,
        });
    }

    private void ShowSettings()
    {
        if (_ownerForm is not HiddenHostForm host)
        {
            return;
        }

        using var form = new SettingsForm(
            _settingsStore,
            _paths,
            _settings,
            _ffmpeg.ExecutablePath ?? "",
            host);
        form.ShowDialog(_ownerForm);
    }

    private void ShowSegmentLog()
    {
        using var form = new SegmentLogForm(_paths);
        form.ShowDialog(_ownerForm);
    }

    private void RefreshStatusMenu()
    {
        var snapshot = _statusStore.Read();
        var formatted = FormatStatus(snapshot);
        _statusItem.Text = formatted.Display;
        _statusItem.ToolTipText = formatted.ToolTip ?? string.Empty;
        UpdateTransportMenu(snapshot);
    }

    private void UpdateTransportMenu(StatusSnapshot? snapshot)
    {
        var state = snapshot?.State?.Trim().ToLowerInvariant() ?? "";
        var recording = snapshot?.Recording ?? false;
        var showStop = state == CaptureStates.Recording && recording;
        var showStart = !showStop;
        _pauseItem.Visible = showStop;
        _pauseItem.Enabled = showStop;
        _resumeItem.Visible = showStart;
        _resumeItem.Enabled = showStart;
    }

    private static FormattedStatus FormatStatus(StatusSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return new FormattedStatus("Status unavailable", null);
        }

        var state = snapshot.State?.Trim() ?? "";
        string text;
        string? fullForTooltip = null;

        switch (state.ToLowerInvariant())
        {
            case CaptureStates.Recording:
                text = snapshot.Recording
                    ? $"Recording · segment {snapshot.CurrentSegmentIndex}"
                    : "Recording";
                break;
            case CaptureStates.Paused:
                text = TrayUi.StatusRecordingStopped;
                break;
            case CaptureStates.Idle:
                text = "Idle";
                if (!snapshot.FfmpegOk && !string.IsNullOrWhiteSpace(snapshot.Error))
                {
                    text += " (FFmpeg unavailable)";
                }

                break;
            case CaptureStates.Error:
                if (string.IsNullOrWhiteSpace(snapshot.Error))
                {
                    text = "Error";
                }
                else
                {
                    text = $"Error: {Truncate(snapshot.Error, MaxErrorMenuLength)}";
                    if (snapshot.Error.Length > MaxErrorMenuLength)
                    {
                        fullForTooltip = snapshot.Error;
                    }
                }

                break;
            default:
                text = string.IsNullOrWhiteSpace(state) ? "State: unknown" : $"State: {state}";
                break;
        }

        return new FormattedStatus(text, fullForTooltip);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 1)] + "…";
    }

    private readonly record struct FormattedStatus(string Display, string? ToolTip);
}

/// <summary>Tray menu strings aligned with Hook <c>mnemonic_constants.gd</c>.</summary>
internal static class TrayUi
{
    public const string StopRecording = "Stop recording";
    public const string StartRecording = "Start recording";
    public const string StatusRecordingStopped = "Recording stopped";
    public const string TooltipStopRecording =
        "Stop recording and quit Mnemonic.";
    public const string TooltipStartRecording =
        "Start recording. Launches Mnemonic if it is not running.";
}
