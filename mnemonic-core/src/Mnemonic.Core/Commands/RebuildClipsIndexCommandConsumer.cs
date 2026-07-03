using Mnemonic.Ipc;
using Mnemonic.Retention;

namespace Mnemonic.Commands;

public sealed class RebuildClipsIndexCommandConsumer
{
    private readonly DataRootPaths _paths;
    private readonly ClipIndexService _clipIndex;

    public RebuildClipsIndexCommandConsumer(DataRootPaths paths, ClipIndexService clipIndex)
    {
        _paths = paths;
        _clipIndex = clipIndex;
    }

    public bool TryConsume()
    {
        return CommandFileHelper.TryConsume<RebuildClipsIndexCommand>(
            _paths.RebuildClipsIndexFile,
            "rebuild_clips_index",
            () => _clipIndex.Rebuild()) == CommandConsumeResult.Success;
    }
}
