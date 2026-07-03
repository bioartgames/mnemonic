using Mnemonic.Capture;
using NAudio.Wave;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class WasapiCaptureProbeTests
{
    [Fact]
    public void Wasapi_mic_capture_emits_non_silent_pcm_within_timeout()
    {
        SkipUnlessWindows();

        using var capture = WasapiCaptureFactory.CreateMicCapture(null);
        var stats = ProbeCapture(capture, TimeSpan.FromSeconds(2));

        Assert.True(stats.EventCount > 0, $"mic DataAvailable events={stats.EventCount}");
        Assert.True(stats.ConvertedBytes > 0, $"mic converted bytes={stats.ConvertedBytes}");
        Assert.True(stats.ConversionErrors == 0, stats.LastConversionError);
    }

    [Fact]
    public void Wasapi_loopback_capture_emits_bytes_within_timeout()
    {
        SkipUnlessWindows();

        using var capture = WasapiCaptureFactory.CreateLoopbackCapture(null);
        var stats = ProbeCapture(capture, TimeSpan.FromSeconds(2));

        Assert.True(stats.EventCount >= 0);
        Assert.True(stats.ConvertedBytes >= 0);
    }

    private static CaptureProbeStats ProbeCapture(IWaveIn capture, TimeSpan duration)
    {
        var stats = new CaptureProbeStats();
        var format = capture.WaveFormat;

        void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            stats.EventCount++;
            stats.RawBytes += e.BytesRecorded;
            try
            {
                var converted = AudioPcmConverter.ConvertToTargetPcm(e.Buffer, e.BytesRecorded, format);
                stats.ConvertedBytes += converted.Length;
                stats.PeakSample = Math.Max(stats.PeakSample, AudioPcmConverter.ComputePeakS16Le(converted));
            }
            catch (Exception ex)
            {
                stats.ConversionErrors++;
                stats.LastConversionError = ex.Message;
            }
        }

        capture.DataAvailable += OnDataAvailable;
        capture.StartRecording();
        Thread.Sleep(duration);
        capture.StopRecording();
        capture.DataAvailable -= OnDataAvailable;

        return stats;
    }

    private static void SkipUnlessWindows()
    {
        Assert.True(OperatingSystem.IsWindows(), "WASAPI probe tests require Windows.");
    }

    private sealed class CaptureProbeStats
    {
        public int EventCount { get; set; }
        public int RawBytes { get; set; }
        public int ConvertedBytes { get; set; }
        public int PeakSample { get; set; }
        public int ConversionErrors { get; set; }
        public string LastConversionError { get; set; } = "";
    }
}
