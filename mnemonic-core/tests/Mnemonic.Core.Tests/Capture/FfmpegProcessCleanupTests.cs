using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class FfmpegProcessCleanupTests
{
    [Fact]
    public void KillBundledOrphans_null_path_is_noop()
    {
        FfmpegProcessCleanup.KillBundledOrphans(null);
    }

    [Fact]
    public void KillBundledOrphans_missing_file_is_noop()
    {
        FfmpegProcessCleanup.KillBundledOrphans(@"C:\nonexistent\ffmpeg.exe");
    }
}
