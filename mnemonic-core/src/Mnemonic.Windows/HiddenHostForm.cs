using Mnemonic;
using Mnemonic.Capture;
using Mnemonic.Commands;
using Mnemonic.Events;
using Mnemonic.Git;
using Mnemonic.Ipc;
using Mnemonic.Retention;
using Mnemonic.Windows.Commands;
using Mnemonic.Windows.Tray;

namespace Mnemonic.Windows;

internal sealed class HiddenHostForm : Form
{
    private readonly CoreBootstrap.Result _bootstrap;
    private readonly StatusStore _statusStore;

    private CaptureService? _capture;
    private ManualPreserveTracker? _preserveTracker;
    private ClipPreserver? _clipPreserver;
    private FlagCommandConsumer? _flagConsumer;
    private CommandPoller? _commandPoller;
    private EventIngestService? _eventIngest;
    private GitPollService? _gitPoll;
    private LiveClipPreviewBuilder? _liveClipPreviewBuilder;

    private System.Windows.Forms.Timer? _segmentPollTimer;
    private System.Windows.Forms.Timer? _processPollTimer;
    private System.Windows.Forms.Timer? _eventPollTimer;
    private System.Windows.Forms.Timer? _livePreviewTimer;
    private System.Windows.Forms.Timer? _scratchCapTimer;
    private System.Windows.Forms.Timer? _scratchStaleTimer;
    private System.Windows.Forms.Timer? _gitPollTimer;
    private TrayController? _tray;
    private SettingsStore? _settingsStore;
    private bool _shutdownDone;

    public HiddenHostForm(CoreBootstrap.Result bootstrap)
    {
        _bootstrap = bootstrap;
        _statusStore = new StatusStore(bootstrap.Paths);

        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Opacity = 0;
        Load += OnLoad;
        FormClosed += OnFormClosed;
        Application.ApplicationExit += OnApplicationExit;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        Hide();

        _preserveTracker = new ManualPreserveTracker();
        _settingsStore = new SettingsStore(_bootstrap.Paths);
        _settingsStore.TryMergeHookOwnedFieldsFromDisk(_bootstrap.Settings);
        _eventIngest = new EventIngestService(_bootstrap.Paths, _bootstrap.Settings, _settingsStore);
        _gitPoll = new GitPollService(_bootstrap.Paths, new GitProcessRunner(), _eventIngest);
        _gitPoll.InitializeBaseline();
        var clipIndex = new ClipIndexService(_bootstrap.Paths);
        _clipPreserver = new ClipPreserver(
            _bootstrap.Paths,
            _preserveTracker,
            _eventIngest,
            _gitPoll,
            _bootstrap.Settings,
            clipIndex,
            _statusStore,
            _bootstrap.Ffmpeg);
        clipIndex.Rebuild();
        _capture = new CaptureService(
            _bootstrap.Paths,
            _statusStore,
            _bootstrap.Settings,
            _bootstrap.Ffmpeg);
        _capture.SegmentClosed += _clipPreserver.HandleSegmentClosed;
        _capture.SegmentClosed += _eventIngest.OnSegmentClosed;
        _liveClipPreviewBuilder = new LiveClipPreviewBuilder(
            _capture,
            _eventIngest,
            () => _gitPoll.CurrentSnapshot,
            _bootstrap.Settings);

        _eventPollTimer = new System.Windows.Forms.Timer { Interval = MnemonicConstants.EventTailPollIntervalMs };
        _eventPollTimer.Tick += (_, _) => _eventIngest.Poll();
        _livePreviewTimer = new System.Windows.Forms.Timer { Interval = MnemonicConstants.EventTailPollIntervalMs };
        _livePreviewTimer.Tick += (_, _) => RefreshLiveClipPreview();

        _flagConsumer = new FlagCommandConsumer(
            _bootstrap.Paths,
            _preserveTracker,
            _capture,
            _statusStore);
        var pauseResumeConsumer = new PauseResumeCommandConsumer(
            _bootstrap.Paths,
            _capture.Pause,
            _capture.Resume);
        var rebuildClipsIndexConsumer = new RebuildClipsIndexCommandConsumer(_bootstrap.Paths, clipIndex);
        var exitCoreConsumer = new ExitCoreCommandConsumer(
            _bootstrap.Paths,
            ShutdownAndExit);
        _commandPoller = new CommandPoller(
            exitCoreConsumer,
            pauseResumeConsumer,
            rebuildClipsIndexConsumer,
            _flagConsumer);

        _segmentPollTimer = new System.Windows.Forms.Timer { Interval = MnemonicConstants.SegmentPollIntervalMs };
        _segmentPollTimer.Tick += (_, _) => _capture.PollSegments();

        _processPollTimer = new System.Windows.Forms.Timer { Interval = MnemonicConstants.ProcessPollIntervalMs };
        _processPollTimer.Tick += (_, _) => _capture.PollProcess();

        _scratchCapTimer = new System.Windows.Forms.Timer { Interval = MnemonicConstants.ScratchCapPollIntervalMs };
        _scratchCapTimer.Tick += (_, _) =>
        {
            var capBytes = ScratchCapPolicy.ToCapBytes(_bootstrap.Settings.ScratchCapGb);
            ScratchCapEnforcer.Enforce(_bootstrap.Paths.ScratchDir, capBytes);
        };

        _scratchStaleTimer = new System.Windows.Forms.Timer { Interval = MnemonicConstants.ScratchStaleCleanupIntervalMs };
        _scratchStaleTimer.Tick += (_, _) => RunScratchStaleCleanup();

        _gitPollTimer = new System.Windows.Forms.Timer { Interval = MnemonicConstants.GitPollIntervalMs };
        _gitPollTimer.Tick += (_, _) => _gitPoll!.Tick();

        FfmpegProcessCleanup.KillBundledOrphans(_bootstrap.Ffmpeg.ExecutablePath);
        if (_bootstrap.Settings.StartRecordingOnLaunch)
        {
            _capture.Start();
        }
        else
        {
            _capture.Pause();
        }
        RunScratchStaleCleanup();
        _segmentPollTimer.Start();
        _processPollTimer.Start();
        _eventPollTimer.Start();
        _livePreviewTimer.Start();
        _scratchCapTimer.Start();
        _scratchStaleTimer.Start();
        _gitPollTimer.Start();
        _commandPoller.Start();

        _tray = new TrayController(
            _bootstrap.Paths,
            _statusStore,
            _settingsStore,
            _bootstrap.Settings,
            _bootstrap.Ffmpeg,
            this);
    }

    internal void RestartCapture()
    {
        if (_capture is null)
        {
            return;
        }

        _capture.Stop();
        _capture.Start();
    }

    internal void StartRecordingSession()
    {
        if (_capture is null)
        {
            return;
        }

        if (!_bootstrap.Settings.StartRecordingOnLaunch)
        {
            _bootstrap.Settings.StartRecordingOnLaunch = true;
            _settingsStore?.Save(_bootstrap.Settings);
        }

        _capture.Resume();
    }

    internal void StopRecordingSession() => ShutdownAndExit();

    public void ShutdownAndExit()
    {
        ShutdownCapture();
        Close();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        ShutdownCapture();
    }

    private void OnApplicationExit(object? sender, EventArgs e)
    {
        ShutdownCapture();
    }

    private void ShutdownCapture()
    {
        if (_shutdownDone)
        {
            return;
        }

        _shutdownDone = true;

        _tray?.Dispose();
        _tray = null;

        _segmentPollTimer?.Stop();
        _processPollTimer?.Stop();
        _eventPollTimer?.Stop();
        _livePreviewTimer?.Stop();
        _scratchCapTimer?.Stop();
        _scratchStaleTimer?.Stop();
        _gitPollTimer?.Stop();
        _commandPoller?.Dispose();

        if (_capture is null)
        {
            FfmpegProcessCleanup.KillBundledOrphans(_bootstrap.Ffmpeg.ExecutablePath);
            return;
        }

        _capture.SegmentClosed -= _clipPreserver!.HandleSegmentClosed;
        if (_eventIngest is not null)
        {
            _capture.SegmentClosed -= _eventIngest.OnSegmentClosed;
        }

        _capture.Dispose();
        _capture = null;
    }

    private void RefreshLiveClipPreview()
    {
        if (_liveClipPreviewBuilder is null)
        {
            return;
        }

        try
        {
            _statusStore.WriteLiveClipPreview(_liveClipPreviewBuilder.Build());
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    private void RunScratchStaleCleanup()
    {
        if (_capture is null)
        {
            return;
        }

        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var logPath = Path.Combine(_bootstrap.Paths.LogsDir, MnemonicConstants.ScratchStaleCleanupLogFileName);
            ScratchStaleCleanup.Enforce(
                _bootstrap.Paths.ScratchDir,
                _capture.IsRecording,
                _capture.ActiveCapturePrefix,
                _capture.CurrentSegmentIndex,
                now,
                deleted => File.AppendAllText(
                    logPath,
                    $"{DateTimeOffset.UtcNow:O} deleted {deleted}{Environment.NewLine}"));
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }
}
