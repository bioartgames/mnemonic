using NAudio.CoreAudioApi;

namespace Mnemonic.Devices;

public static class WasapiDeviceLister
{
    public static IReadOnlyList<AudioDeviceOption> ListCaptureDevices() =>
        WithDefault(Enumerate(DataFlow.Capture, AudioDeviceKind.Capture));

    public static IReadOnlyList<AudioDeviceOption> ListRenderDevices() =>
        WithDefault(Enumerate(DataFlow.Render, AudioDeviceKind.Loopback));

    private static List<AudioDeviceOption> Enumerate(DataFlow flow, AudioDeviceKind kind)
    {
        var devices = new List<AudioDeviceOption>();
        using var enumerator = new MMDeviceEnumerator();
        foreach (var device in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            devices.Add(new AudioDeviceOption(device.ID, device.FriendlyName, kind));
        }

        return devices;
    }

    private static IReadOnlyList<AudioDeviceOption> WithDefault(IReadOnlyList<AudioDeviceOption> devices)
    {
        var result = new List<AudioDeviceOption> { new("", "(System default)") };
        result.AddRange(devices);
        return result;
    }
}
