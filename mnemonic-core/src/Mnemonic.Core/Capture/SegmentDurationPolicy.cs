namespace Mnemonic.Capture;

public static class SegmentDurationPolicy
{
    public const int MinSeconds = 30;
    public const int MaxSeconds = 600;

    public static int ClampSeconds(int raw) => Math.Clamp(raw, MinSeconds, MaxSeconds);

    public static int Normalize(int raw)
    {
        if (raw <= 0)
        {
            return MnemonicConstants.SegmentDurationSeconds;
        }

        return ClampSeconds(raw);
    }
}
