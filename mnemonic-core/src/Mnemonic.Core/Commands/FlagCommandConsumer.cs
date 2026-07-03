using Mnemonic.Capture;
using Mnemonic.Ipc;
using Mnemonic.Retention;

namespace Mnemonic.Commands;

public sealed class FlagCommandConsumer
{
    private readonly DataRootPaths _paths;
    private readonly ManualPreserveTracker _tracker;
    private readonly CaptureService _capture;
    private readonly StatusStore _statusStore;

    public FlagCommandConsumer(
        DataRootPaths paths,
        ManualPreserveTracker tracker,
        CaptureService capture,
        StatusStore statusStore)
    {
        _paths = paths;
        _tracker = tracker;
        _capture = capture;
        _statusStore = statusStore;
    }

    public bool TryConsume()
    {
        return CommandFileHelper.TryConsume<FlagCommand>(
            _paths.FlagCurrentFile,
            "flag_current",
            () =>
            {
                var segmentIndex = _capture.CurrentSegmentIndex;
                _tracker.RequestPreserve(segmentIndex);
                _statusStore.SetPendingManualPreserve(segmentIndex);
            }) == CommandConsumeResult.Success;
    }
}
