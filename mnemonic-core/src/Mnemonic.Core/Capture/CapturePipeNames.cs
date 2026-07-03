namespace Mnemonic.Capture;

public static class CapturePipeNames
{
    public static string GetMicPipeName(string sessionPrefix) => $"mnemonic_{sessionPrefix}_mic";

    public static string GetDesktopPipeName(string sessionPrefix) => $"mnemonic_{sessionPrefix}_desktop";

    public static string GetWin32Path(string pipeName) => $@"\\.\pipe\{pipeName}";
}
