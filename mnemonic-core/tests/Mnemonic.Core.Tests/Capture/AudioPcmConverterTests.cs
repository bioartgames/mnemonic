using Mnemonic.Capture;
using NAudio;
using NAudio.Wave;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class AudioPcmConverterTests
{
    [Fact]
    public void Convert_ieee_float_buffer_produces_non_silent_pcm()
    {
        var sourceFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var source = BuildSineFloat(sourceFormat, sampleCount: 4800);

        var converted = AudioPcmConverter.ConvertToTargetPcm(source, source.Length, sourceFormat);

        Assert.True(converted.Length > 0);
        Assert.True(AudioPcmConverter.ComputePeakS16Le(converted) > 500);
    }

    [Fact]
    public void Convert_pcm_48k_stereo_passes_through()
    {
        var sourceFormat = new WaveFormat(48000, 16, 2);
        var source = BuildSinePcm16(sourceFormat, sampleCount: 4800);

        var converted = AudioPcmConverter.ConvertToTargetPcm(source, source.Length, sourceFormat);

        Assert.Equal(source.Length, converted.Length);
        Assert.True(AudioPcmConverter.ComputePeakS16Le(converted) > 500);
    }

    [Fact]
    public void Legacy_wave_format_conversion_stream_throws_acm_not_possible_for_wasapi_float_chunk()
    {
        var sourceFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var source = BuildSineFloat(sourceFormat, sampleCount: 480);

        var ex = Assert.Throws<MmException>(() =>
            ConvertUsingLegacyWaveFormatConversionStream(source, sourceFormat));

        Assert.Contains("AcmNotPossible", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void New_converter_succeeds_for_same_wasapi_float_segment_that_breaks_legacy_acm()
    {
        var sourceFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var source = BuildSineFloat(sourceFormat, sampleCount: 480);

        var converted = AudioPcmConverter.ConvertToTargetPcm(source, source.Length, sourceFormat);

        Assert.True(converted.Length > 0);
        Assert.True(AudioPcmConverter.ComputePeakS16Le(converted) > 500);
    }

    private static byte[] ConvertUsingLegacyWaveFormatConversionStream(byte[] source, WaveFormat sourceFormat)
    {
        using var input = new RawSourceWaveStream(source, 0, source.Length, sourceFormat);
        using var conversion = new WaveFormatConversionStream(AudioPcmConverter.TargetFormat, input);
        using var output = new MemoryStream();
        var chunk = new byte[4096];
        int read;
        while ((read = conversion.Read(chunk, 0, chunk.Length)) > 0)
        {
            output.Write(chunk, 0, read);
        }

        return output.ToArray();
    }

    private static byte[] BuildSineFloat(WaveFormat format, int sampleCount)
    {
        var bytes = new byte[sampleCount * format.BlockAlign];
        var samples = sampleCount;
        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)format.SampleRate;
            var value = (float)(0.25 * Math.Sin(2 * Math.PI * 440 * t));
            for (var ch = 0; ch < format.Channels; ch++)
            {
                var offset = i * format.BlockAlign + ch * 4;
                BitConverter.TryWriteBytes(bytes.AsSpan(offset, 4), value);
            }
        }

        return bytes;
    }

    private static byte[] BuildSinePcm16(WaveFormat format, int sampleCount)
    {
        var bytes = new byte[sampleCount * format.BlockAlign];
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)format.SampleRate;
            var value = (short)(8000 * Math.Sin(2 * Math.PI * 440 * t));
            for (var ch = 0; ch < format.Channels; ch++)
            {
                var offset = i * format.BlockAlign + ch * 2;
                BitConverter.TryWriteBytes(bytes.AsSpan(offset, 2), value);
            }
        }

        return bytes;
    }
}
