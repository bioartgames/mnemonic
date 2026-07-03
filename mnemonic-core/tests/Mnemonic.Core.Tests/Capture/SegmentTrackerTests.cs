using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class SegmentTrackerTests
{
    [Fact]
    public void TryParseScratchFileName_parses_valid_name()
    {
        Assert.True(SegmentTracker.TryParseScratchFileName(
            "mn_1234567890_ab12_segment_00003.mp4",
            out var prefix,
            out var index));
        Assert.Equal("mn_1234567890_ab12", prefix);
        Assert.Equal(3, index);
    }

    [Fact]
    public void TryParseScratchFileName_rejects_non_mp4()
    {
        Assert.False(SegmentTracker.TryParseScratchFileName("mn_1_ab12_segment_00003.mkv", out _, out _));
    }

    [Fact]
    public void TryParseScratchFileName_rejects_missing_marker()
    {
        Assert.False(SegmentTracker.TryParseScratchFileName("random.mp4", out _, out _));
    }

    [Fact]
    public void TryParseScratchFileName_rejects_bad_digits()
    {
        Assert.False(SegmentTracker.TryParseScratchFileName("mn_1_ab12_segment_abcde.mp4", out _, out _));
    }
}
