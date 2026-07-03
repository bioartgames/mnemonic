using Mnemonic.Ipc.Models;
using Mnemonic.Retention;
using Xunit;

namespace Mnemonic.Core.Tests.Retention;

public sealed class ScoreTierNormalizerTests
{
    [Fact]
    public void Normalize_bumps_highlight_above_preserve()
    {
        var settings = new AppSettings
        {
            PreserveThreshold = 25,
            HighlightScoreMin = 20,
        };

        var result = ScoreTierNormalizer.Normalize(settings);

        Assert.Equal(25, result.PreserveThreshold);
        Assert.Equal(26, result.HighlightScoreMin);
    }

    [Fact]
    public void Normalize_clamps_notable_between_preserve_and_highlight()
    {
        var settings = new AppSettings
        {
            PreserveThreshold = 10,
            NotableScoreMin = 8,
            HighlightScoreMin = 12,
        };

        var result = ScoreTierNormalizer.Normalize(settings);

        Assert.Equal(10, result.NotableScoreMin);
        Assert.Equal(12, result.HighlightScoreMin);
    }

    [Fact]
    public void Normalize_defaults_notable_to_preserve_when_zero()
    {
        var settings = new AppSettings
        {
            PreserveThreshold = 12,
            NotableScoreMin = 0,
            HighlightScoreMin = 25,
        };

        var result = ScoreTierNormalizer.Normalize(settings);

        Assert.Equal(12, result.NotableScoreMin);
    }

    [Fact]
    public void ResolveHighlightMin_matches_sanitizer_behavior()
    {
        Assert.Equal(26, ScoreTierNormalizer.ResolveHighlightMin(25, 20));
    }
}
