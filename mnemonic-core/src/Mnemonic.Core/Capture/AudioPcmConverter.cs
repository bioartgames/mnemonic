using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mnemonic.Capture;

internal static class AudioPcmConverter
{
    internal static readonly WaveFormat TargetFormat = new(
        MnemonicConstants.CaptureAudioSampleRate,
        16,
        MnemonicConstants.CaptureAudioChannels);

    internal static byte[] ConvertToTargetPcm(byte[] buffer, int bytesRecorded, WaveFormat sourceFormat)
    {
        if (bytesRecorded <= 0)
        {
            return [];
        }

        if (WaveFormatMatchesTarget(sourceFormat))
        {
            var copy = new byte[bytesRecorded];
            Array.Copy(buffer, copy, bytesRecorded);
            return copy;
        }

        using var inputStream = new RawSourceWaveStream(buffer, 0, bytesRecorded, sourceFormat);
        ISampleProvider sampleProvider = new WaveToSampleProvider(inputStream);

        if (sampleProvider.WaveFormat.SampleRate != TargetFormat.SampleRate)
        {
            sampleProvider = new WdlResamplingSampleProvider(sampleProvider, TargetFormat.SampleRate);
        }

        if (sampleProvider.WaveFormat.Channels != TargetFormat.Channels)
        {
            sampleProvider = sampleProvider.ToStereo();
        }

        var output = new SampleToWaveProvider16(sampleProvider);
        using var memory = new MemoryStream();
        var chunk = new byte[4096];
        int read;
        while ((read = output.Read(chunk, 0, chunk.Length)) > 0)
        {
            memory.Write(chunk, 0, read);
        }

        return memory.ToArray();
    }

    internal static int ComputePeakS16Le(IReadOnlyList<byte> pcm)
    {
        if (pcm.Count < 2)
        {
            return 0;
        }

        var peak = 0;
        for (var i = 0; i + 1 < pcm.Count; i += 2)
        {
            var sample = (short)(pcm[i] | (pcm[i + 1] << 8));
            var abs = Math.Abs(sample);
            if (abs > peak)
            {
                peak = abs;
            }
        }

        return peak;
    }

    private static bool WaveFormatMatchesTarget(WaveFormat format) =>
        format.Encoding == WaveFormatEncoding.Pcm
        && format.SampleRate == TargetFormat.SampleRate
        && format.Channels == TargetFormat.Channels
        && format.BitsPerSample == TargetFormat.BitsPerSample;
}
