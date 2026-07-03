using Mnemonic.Commands;

namespace Mnemonic.Windows.Commands;

/// <summary>
/// WinForms timer that polls pause, resume, rebuild clips index, and flag commands.
/// </summary>
internal sealed class CommandPoller : IDisposable
{
    private readonly ExitCoreCommandConsumer _exitCoreConsumer;
    private readonly PauseResumeCommandConsumer _pauseResumeConsumer;
    private readonly RebuildClipsIndexCommandConsumer _rebuildClipsIndexConsumer;
    private readonly FlagCommandConsumer _flagConsumer;
    private System.Windows.Forms.Timer? _timer;

    public CommandPoller(
        ExitCoreCommandConsumer exitCoreConsumer,
        PauseResumeCommandConsumer pauseResumeConsumer,
        RebuildClipsIndexCommandConsumer rebuildClipsIndexConsumer,
        FlagCommandConsumer flagConsumer)
    {
        _exitCoreConsumer = exitCoreConsumer;
        _pauseResumeConsumer = pauseResumeConsumer;
        _rebuildClipsIndexConsumer = rebuildClipsIndexConsumer;
        _flagConsumer = flagConsumer;
    }

    public void Start()
    {
        _timer ??= new System.Windows.Forms.Timer { Interval = MnemonicConstants.CommandPollIntervalMs };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer is null)
            return;
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    public void Dispose()
    {
        Stop();
        _timer?.Dispose();
        _timer = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_exitCoreConsumer.TryConsume())
        {
            return;
        }

        _pauseResumeConsumer.TryConsume();
        _rebuildClipsIndexConsumer.TryConsume();
        _flagConsumer.TryConsume();
    }
}
