namespace Mnemonic.Devices;

public enum AudioDeviceKind
{
    Capture,
    Loopback,
}

public sealed record AudioDeviceOption(string Id, string DisplayName, AudioDeviceKind? Kind = null);
