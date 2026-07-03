namespace Mnemonic.Ipc;

public static class SmokeBootstrap
{
    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(MnemonicConstants.SmokeSeedAudioEnvironmentVariable),
            "1",
            StringComparison.Ordinal);
}
