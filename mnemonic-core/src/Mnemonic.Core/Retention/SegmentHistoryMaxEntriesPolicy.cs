namespace Mnemonic.Retention;

public static class SegmentHistoryMaxEntriesPolicy
{
    public const int Min = 10;
    public const int Max = 1000;
    public const int Default = 200;

    public static int Clamp(int raw) => Math.Clamp(raw, Min, Max);
}
