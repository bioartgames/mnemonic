using Mnemonic.Ipc;

namespace Mnemonic.Commands;

public sealed class PauseResumeCommandConsumer
{
    private readonly DataRootPaths _paths;
    private readonly Action _pause;
    private readonly Action _resume;

    public PauseResumeCommandConsumer(DataRootPaths paths, Action pause, Action resume)
    {
        _paths = paths;
        _pause = pause;
        _resume = resume;
    }

    public bool TryConsume()
    {
        if (TryConsumePause())
        {
            return true;
        }

        return TryConsumeResume();
    }

    private bool TryConsumePause()
    {
        return CommandFileHelper.TryConsume<PauseCaptureCommand>(
            _paths.PauseCaptureFile,
            "pause_capture",
            _pause) == CommandConsumeResult.Success;
    }

    private bool TryConsumeResume()
    {
        return CommandFileHelper.TryConsume<ResumeCaptureCommand>(
            _paths.ResumeCaptureFile,
            "resume_capture",
            _resume) == CommandConsumeResult.Success;
    }
}
