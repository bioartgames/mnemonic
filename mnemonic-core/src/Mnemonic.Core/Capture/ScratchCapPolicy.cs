namespace Mnemonic.Capture;

public static class ScratchCapPolicy
{
    public const int MinGb = 1;
    public const int MaxGb = 128;

    public static int ClampGb(int raw) => Math.Clamp(raw, MinGb, MaxGb);

    public static long ToCapBytes(int scratchCapGb)
    {
        var bytes = (long)ClampGb(scratchCapGb) * MnemonicConstants.ScratchCapBytesPerGb;
        return bytes < MnemonicConstants.ScratchCapMinBytes
            ? MnemonicConstants.ScratchCapMinBytes
            : bytes;
    }
}
