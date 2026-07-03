namespace Mnemonic.Capture;

public static class CaptureAudioLayout
{
    public static IReadOnlyList<AudioTrackDescriptor> GetTrackDescriptors(CaptureAudioConfig config)
    {
        if (!config.HasAnyAudio)
        {
            return [];
        }

        var tracks = new List<AudioTrackDescriptor>();
        if (config.HasMic)
        {
            tracks.Add(new AudioTrackDescriptor
            {
                Role = "mic",
                StreamIndex = 1,
                Codec = "aac",
            });
        }

        if (config.HasDesktop)
        {
            tracks.Add(new AudioTrackDescriptor
            {
                Role = "desktop",
                StreamIndex = config.HasMic ? 2 : 1,
                Codec = "aac",
            });
        }

        return tracks;
    }
}
