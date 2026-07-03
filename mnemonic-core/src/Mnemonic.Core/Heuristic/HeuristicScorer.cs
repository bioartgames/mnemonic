using Mnemonic.Events;
using Mnemonic.Ipc.Models;

namespace Mnemonic.Heuristic;

public static class HeuristicScorer
{
    public static int Score(IReadOnlyList<SessionEvent> entries) => Score(entries, settings: null);

    public static int Score(IReadOnlyList<SessionEvent> entries, AppSettings? settings) =>
        ScoreBreakdown(entries, settings).Total;

    public static (int Total, IReadOnlyList<HeuristicScoreLine> Lines) ScoreBreakdown(
        IReadOnlyList<SessionEvent> entries,
        AppSettings? settings)
    {
        var perTypeCount = new Dictionary<string, int>(StringComparer.Ordinal);
        var perTypePoints = new Dictionary<string, int>(StringComparer.Ordinal);
        var perTypeDetail = new Dictionary<string, string?>(StringComparer.Ordinal);
        var sum = 0;

        foreach (var e in entries)
        {
            var weight = HeuristicSettingsResolver.ResolveWeight(e.Type, settings);
            if (weight <= 0)
            {
                continue;
            }

            var cap = HeuristicSettingsResolver.ResolveCap(e.Type, settings);
            if (cap <= 0)
            {
                sum += weight;
                perTypeCount.TryGetValue(e.Type, out var uncappedCount);
                perTypeCount[e.Type] = uncappedCount + 1;
                perTypePoints.TryGetValue(e.Type, out var uncappedPoints);
                perTypePoints[e.Type] = uncappedPoints + weight;
                CapturePatternDetail(e, perTypeDetail);
                continue;
            }

            perTypeCount.TryGetValue(e.Type, out var count);
            if (count >= cap)
            {
                continue;
            }

            perTypeCount[e.Type] = count + 1;
            sum += weight;
            perTypePoints.TryGetValue(e.Type, out var points);
            perTypePoints[e.Type] = points + weight;
            CapturePatternDetail(e, perTypeDetail);
        }

        var lines = new List<HeuristicScoreLine>();
        foreach (var pair in perTypePoints.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
        {
            if (pair.Value <= 0)
            {
                continue;
            }

            perTypeCount.TryGetValue(pair.Key, out var lineCount);
            perTypeDetail.TryGetValue(pair.Key, out var detail);
            lines.Add(new HeuristicScoreLine
            {
                Type = pair.Key,
                Count = lineCount,
                Points = pair.Value,
                Detail = detail,
            });
        }

        return (sum, lines);
    }

    private static void CapturePatternDetail(
        SessionEvent e,
        Dictionary<string, string?> perTypeDetail)
    {
        if (perTypeDetail.ContainsKey(e.Type))
        {
            return;
        }

        var detail = SessionEventExtras.GetString(e, "pattern_detail");
        if (!string.IsNullOrWhiteSpace(detail))
        {
            perTypeDetail[e.Type] = detail;
        }
    }
}
