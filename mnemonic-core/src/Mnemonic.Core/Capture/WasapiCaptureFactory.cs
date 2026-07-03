using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Mnemonic.Capture;

public static class WasapiCaptureFactory
{
    public static IWaveIn CreateMicCapture(string? endpointId)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
        {
            return new WasapiCapture();
        }

        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDevice(endpointId);
        return new WasapiCapture(device);
    }

    public static IWaveIn CreateLoopbackCapture(string? renderEndpointId)
    {
        if (string.IsNullOrWhiteSpace(renderEndpointId))
        {
            return new WasapiLoopbackCapture();
        }

        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDevice(renderEndpointId);
        return new WasapiLoopbackCapture(device);
    }
}
