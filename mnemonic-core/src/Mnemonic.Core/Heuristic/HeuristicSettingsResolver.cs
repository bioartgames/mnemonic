using Mnemonic.Ipc.Models;

namespace Mnemonic.Heuristic;

public static class HeuristicSettingsResolver
{
    public const int MaxWeight = 20;
    public const int MaxCap = 20;

    public static int ResolveWeight(string type, AppSettings? settings)
    {
        var entry = HeuristicCatalog.TryGet(type);
        if (entry is null)
        {
            return 0;
        }

        if (settings?.Heuristics is not null
            && settings.Heuristics.TryGetValue(type, out var typeSettings)
            && typeSettings is not null)
        {
            if (!typeSettings.Enabled || typeSettings.Weight <= 0)
            {
                return 0;
            }

            return Math.Clamp(typeSettings.Weight, 0, MaxWeight);
        }

        return entry.DefaultWeight;
    }

    public static int ResolveCap(string type, AppSettings? settings)
    {
        var entry = HeuristicCatalog.TryGet(type);
        if (entry is null)
        {
            return 0;
        }

        if (settings?.Heuristics is not null
            && settings.Heuristics.TryGetValue(type, out var typeSettings)
            && typeSettings is not null
            && typeSettings.Cap > 0)
        {
            return Math.Clamp(typeSettings.Cap, 0, MaxCap);
        }

        return entry.DefaultCap;
    }
}
