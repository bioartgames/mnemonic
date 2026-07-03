namespace Mnemonic.Capture;

public static class FfmpegBundlePaths
{
    public static string GetBundledRoot(string applicationBaseDirectory)
    {
        return Path.Combine(applicationBaseDirectory, MnemonicConstants.FfmpegRelativeDir);
    }

    public static string GetBundledExecutablePath(string applicationBaseDirectory)
    {
        return Path.Combine(applicationBaseDirectory, MnemonicConstants.FfmpegExecutableRelativePath);
    }
}
