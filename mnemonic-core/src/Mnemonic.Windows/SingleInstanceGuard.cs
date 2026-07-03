namespace Mnemonic.Windows;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    public bool IsPrimaryInstance { get; }

    public SingleInstanceGuard()
    {
        _mutex = new Mutex(initiallyOwned: true, name: MnemonicConstants.MutexName, createdNew: out bool created);
        IsPrimaryInstance = created;
    }

    public void Dispose() => _mutex.Dispose();
}
