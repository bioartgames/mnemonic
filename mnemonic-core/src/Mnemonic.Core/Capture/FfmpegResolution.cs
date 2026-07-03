namespace Mnemonic.Capture;

public sealed record FfmpegResolution(
    string? ExecutablePath,
    bool IsAvailable,
    string ErrorMessage);
