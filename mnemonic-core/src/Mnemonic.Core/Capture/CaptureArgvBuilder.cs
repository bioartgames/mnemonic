namespace Mnemonic.Capture;

public static class CaptureArgvBuilder
{
    private static readonly string[] VideoCodecArgs =
    [
        "-c:v", "libx264",
        "-preset", "ultrafast",
        "-crf", "28",
        "-pix_fmt", "yuv420p",
        "-r", MnemonicConstants.CaptureVideoFramerate.ToString(),
    ];

    public static IReadOnlyList<string> Build(
        CaptureAudioConfig config,
        bool drawMouse,
        int segmentDurationSeconds,
        string segmentOutfilePattern,
        string sessionPrefix)
    {
        var segmentTime = segmentDurationSeconds.ToString();
        var videoFilter = BuildVideoFilter(drawMouse);
        var argv = new List<string>
        {
            "-hide_banner",
            "-loglevel", "warning",
            "-filter_complex", videoFilter,
        };

        if (config.HasMic)
        {
            argv.AddRange(BuildPipeInputArgs(CapturePipeNames.GetWin32Path(CapturePipeNames.GetMicPipeName(sessionPrefix))));
        }

        if (config.HasDesktop)
        {
            argv.AddRange(BuildPipeInputArgs(CapturePipeNames.GetWin32Path(CapturePipeNames.GetDesktopPipeName(sessionPrefix))));
        }

        argv.AddRange(["-map", "[vout]"]);
        argv.AddRange(VideoCodecArgs);

        if (config.HasMic && config.HasDesktop)
        {
            argv.AddRange(["-map", "0:a", "-map", "1:a", "-c:a:0", "aac", "-c:a:1", "aac"]);
        }
        else if (config.HasMic || config.HasDesktop)
        {
            argv.AddRange(["-map", "0:a", "-c:a", "aac"]);
        }
        else
        {
            argv.Add("-an");
        }

        argv.AddRange(
        [
            "-f", "segment",
            "-segment_time", segmentTime,
            "-reset_timestamps", "1",
            "-segment_format_options", "movflags=frag_keyframe+empty_moov+default_base_moof",
            segmentOutfilePattern,
        ]);

        return argv;
    }

    private static string BuildVideoFilter(bool drawMouse)
    {
        var captureCursor = drawMouse ? "1" : "0";
        return
            $"gfxcapture=monitor_idx={MnemonicConstants.CaptureMonitorIndex}" +
            $":max_framerate={MnemonicConstants.CaptureVideoFramerate}" +
            $":capture_cursor={captureCursor},hwdownload,format=bgra[vout]";
    }

    private static IEnumerable<string> BuildPipeInputArgs(string pipePath) =>
    [
        "-f", "s16le",
        "-ar", MnemonicConstants.CaptureAudioSampleRate.ToString(),
        "-ac", MnemonicConstants.CaptureAudioChannels.ToString(),
        "-thread_queue_size", MnemonicConstants.CaptureAudioThreadQueueSize.ToString(),
        "-i", pipePath,
    ];
}
