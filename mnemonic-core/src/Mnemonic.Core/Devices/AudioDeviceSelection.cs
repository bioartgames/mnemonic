using Mnemonic.Ipc.Models;

namespace Mnemonic.Devices;

public static class AudioDeviceSelection
{
    public static int FindIndexForId(IReadOnlyList<AudioDeviceOption> devices, string? selectedId)
    {
        if (devices.Count == 0)
        {
            return -1;
        }

        var id = selectedId ?? "";
        for (var i = 0; i < devices.Count; i++)
        {
            if (string.Equals(devices[i].Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }

    public static bool TryGetDeviceAt(
        IReadOnlyList<AudioDeviceOption> devices,
        int selectedIndex,
        out AudioDeviceOption device)
    {
        if (selectedIndex >= 0 && selectedIndex < devices.Count)
        {
            device = devices[selectedIndex];
            return true;
        }

        device = null!;
        return false;
    }

    public static void ApplyMicSelection(AppSettings target, AudioDeviceOption device)
    {
        target.CaptureMicEnabled = true;
        target.MicDeviceId = device.Id;
    }

    public static void ClearMicSelection(AppSettings target)
    {
        target.CaptureMicEnabled = false;
        target.MicDeviceId = "";
    }

    public static void ApplyDesktopSelection(AppSettings target, AudioDeviceOption device)
    {
        target.CaptureDesktopEnabled = true;
        target.DesktopLoopbackDeviceId = device.Id;
    }

    public static void ClearDesktopSelection(AppSettings target)
    {
        target.CaptureDesktopEnabled = false;
        target.DesktopLoopbackDeviceId = "";
    }
}
