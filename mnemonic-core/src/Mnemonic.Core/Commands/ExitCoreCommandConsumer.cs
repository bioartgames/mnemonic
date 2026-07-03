using Mnemonic.Ipc;

namespace Mnemonic.Commands;

public sealed class ExitCoreCommandConsumer
{
    private readonly DataRootPaths _paths;
    private readonly Action _shutdown;

    public ExitCoreCommandConsumer(DataRootPaths paths, Action shutdown)
    {
        _paths = paths;
        _shutdown = shutdown;
    }

    public bool TryConsume() =>
        CommandFileHelper.TryConsume<ExitCoreCommand>(
            _paths.ExitCoreFile,
            "exit_core",
            _shutdown) == CommandConsumeResult.Success;
}
