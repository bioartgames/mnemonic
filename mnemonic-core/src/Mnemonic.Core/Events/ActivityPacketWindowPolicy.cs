using Mnemonic.Capture;

namespace Mnemonic.Events;

public static class ActivityPacketWindowPolicy
{
    public const int MinSeconds = 15;
    public const int MaxSeconds = 120;

    public static int Compute(int segmentDurationSeconds)
    {
        var seg = SegmentDurationPolicy.Normalize(segmentDurationSeconds);
        var half = (int)Math.Round(seg / 2.0, MidpointRounding.AwayFromZero);
        return Math.Clamp(half, MinSeconds, MaxSeconds);
    }
}
