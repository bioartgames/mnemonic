namespace Mnemonic.Retention;

public sealed class ManualPreserveTracker
{
    private int _flaggedIndex = -1;

    public void RequestPreserve(int segmentIndex)
    {
        if (segmentIndex < 0)
        {
            return;
        }

        _flaggedIndex = segmentIndex;
    }

    public bool ShouldPreserve(int segmentIndex)
    {
        return segmentIndex >= 0 && segmentIndex == _flaggedIndex;
    }

    public void Clear(int segmentIndex)
    {
        if (segmentIndex == _flaggedIndex)
        {
            _flaggedIndex = -1;
        }
    }
}
