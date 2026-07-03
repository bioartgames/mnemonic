namespace Mnemonic.Retention;

public static class PreserveThresholdPolicy
{
    public const int Min = 1;
    public const int Max = 500;

    public static int Clamp(int raw) => Math.Clamp(raw, Min, Max);
}
