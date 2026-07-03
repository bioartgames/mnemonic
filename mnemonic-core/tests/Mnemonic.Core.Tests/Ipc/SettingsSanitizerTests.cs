using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class SettingsSanitizerTests
{
    [Fact]
    public void Sanitize_clears_unknown_mic_and_desktop_ids()
    {
        var settings = new AppSettings
        {
            MicDeviceId = "not-a-real-wasapi-endpoint",
            DesktopLoopbackDeviceId = "also-not-real",
        };

        var changed = SettingsSanitizer.Sanitize(settings);

        Assert.True(changed);
        Assert.Equal("", settings.MicDeviceId);
        Assert.Equal("", settings.DesktopLoopbackDeviceId);
        Assert.True(settings.CaptureMicEnabled);
        Assert.True(settings.CaptureDesktopEnabled);
    }

    [Fact]
    public void Sanitize_keeps_empty_ids_unchanged()
    {
        var settings = new AppSettings
        {
            MicDeviceId = "",
            DesktopLoopbackDeviceId = "",
            SegmentDurationSeconds = 120,
            PreserveThreshold = 10,
            NotableScoreMin = 10,
            HighlightScoreMin = MnemonicConstants.SignificanceTierHighlightScoreMin,
            SegmentHistoryMaxEntries = SegmentHistoryMaxEntriesPolicy.Default,
        };

        var changed = SettingsSanitizer.Sanitize(settings);

        Assert.False(changed);
    }

    [Theory]
    [InlineData(29, 30)]
    [InlineData(601, 600)]
    [InlineData(0, 120)]
    public void Sanitize_normalizes_segment_duration(int raw, int expected)
    {
        var settings = new AppSettings { SegmentDurationSeconds = raw };

        var changed = SettingsSanitizer.Sanitize(settings);

        Assert.True(changed);
        Assert.Equal(expected, settings.SegmentDurationSeconds);
    }

    [Fact]
    public void Sanitize_bumps_highlight_score_above_preserve_threshold()
    {
        var settings = new AppSettings
        {
            PreserveThreshold = 25,
            HighlightScoreMin = 20,
        };

        var changed = SettingsSanitizer.Sanitize(settings);

        Assert.True(changed);
        Assert.Equal(26, settings.HighlightScoreMin);
    }

    [Fact]
    public void Sanitize_enforces_preserve_lte_notable_lte_highlight()
    {
        var settings = new AppSettings
        {
            PreserveThreshold = 10,
            NotableScoreMin = 8,
            HighlightScoreMin = 12,
        };

        var changed = SettingsSanitizer.Sanitize(settings);

        Assert.True(changed);
        Assert.Equal(10, settings.NotableScoreMin);
        Assert.Equal(12, settings.HighlightScoreMin);
    }

    [Fact]
    public void Sanitize_defaults_notable_to_preserve_when_unset()
    {
        var settings = new AppSettings
        {
            PreserveThreshold = 12,
            NotableScoreMin = 0,
            HighlightScoreMin = 25,
        };

        var changed = SettingsSanitizer.Sanitize(settings);

        Assert.True(changed);
        Assert.Equal(12, settings.NotableScoreMin);
    }
}
