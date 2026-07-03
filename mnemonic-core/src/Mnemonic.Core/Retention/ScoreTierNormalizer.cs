using Mnemonic.Ipc.Models;

namespace Mnemonic.Retention;

public static class ScoreTierNormalizer
{
    public record Result(int PreserveThreshold, int NotableScoreMin, int HighlightScoreMin);

    public static Result Normalize(AppSettings settings)
    {
        var preserveThreshold = PreserveThresholdPolicy.Clamp(settings.PreserveThreshold);
        var highlightScoreMin = HighlightScoreMinPolicy.Clamp(settings.HighlightScoreMin);
        if (highlightScoreMin <= preserveThreshold)
        {
            highlightScoreMin = Math.Min(
                HighlightScoreMinPolicy.Max,
                preserveThreshold + 1);
        }

        var notableScoreMin = settings.NotableScoreMin > 0
            ? NotableScoreMinPolicy.Clamp(settings.NotableScoreMin)
            : preserveThreshold;
        if (notableScoreMin < preserveThreshold)
        {
            notableScoreMin = preserveThreshold;
        }

        if (notableScoreMin > highlightScoreMin)
        {
            notableScoreMin = highlightScoreMin;
        }

        return new Result(preserveThreshold, notableScoreMin, highlightScoreMin);
    }

    public static int ResolveHighlightMin(int preserveThreshold, int highlightFromSettings)
    {
        var highlight = HighlightScoreMinPolicy.Clamp(highlightFromSettings);
        return highlight <= preserveThreshold
            ? Math.Min(HighlightScoreMinPolicy.Max, preserveThreshold + 1)
            : highlight;
    }

    public static bool ApplyTo(AppSettings settings)
    {
        var before = (settings.PreserveThreshold, settings.NotableScoreMin, settings.HighlightScoreMin);
        var normalized = Normalize(settings);
        settings.PreserveThreshold = normalized.PreserveThreshold;
        settings.NotableScoreMin = normalized.NotableScoreMin;
        settings.HighlightScoreMin = normalized.HighlightScoreMin;
        return before != (settings.PreserveThreshold, settings.NotableScoreMin, settings.HighlightScoreMin);
    }
}
