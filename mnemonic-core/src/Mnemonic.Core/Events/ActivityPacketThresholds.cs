using Mnemonic;

namespace Mnemonic.Events;

public static class ActivityPacketThresholds
{
    public static int MinActions(int windowSec) =>
        Math.Max(1, (int)Math.Ceiling(
            MnemonicConstants.EditIntensityMinActions * (double)windowSec
            / MnemonicConstants.ActivityPacketReferenceWindowSeconds));

    public static int MinTransitions(int windowSec) =>
        Math.Max(1, (int)Math.Ceiling(
            MnemonicConstants.SceneHoppingMinTransitions * (double)windowSec
            / MnemonicConstants.ActivityPacketReferenceWindowSeconds));

    public static int FocusDominantSec(int windowSec) => windowSec / 2;
}
