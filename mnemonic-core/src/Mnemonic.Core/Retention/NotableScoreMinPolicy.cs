namespace Mnemonic.Retention;

public static class NotableScoreMinPolicy
{
    public const int Min = PreserveThresholdPolicy.Min;
    public const int Max = PreserveThresholdPolicy.Max;

    public static int Clamp(int raw) => Math.Clamp(raw, Min, Max);
}
