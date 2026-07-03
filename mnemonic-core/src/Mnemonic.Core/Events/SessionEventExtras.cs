using System.Text.Json;

namespace Mnemonic.Events;

public static class SessionEventExtras
{
    public static string? GetString(SessionEvent e, string key)
    {
        if (e.Extra is null || !e.Extra.TryGetValue(key, out var el))
        {
            return null;
        }

        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }

    public static int GetInt(SessionEvent e, string key, int defaultValue = 0)
    {
        if (e.Extra is null || !e.Extra.TryGetValue(key, out var el))
        {
            return defaultValue;
        }

        return el.TryGetInt32(out var value) ? value : defaultValue;
    }

    public static double GetDouble(SessionEvent e, string key, double defaultValue = 0.0)
    {
        if (e.Extra is null || !e.Extra.TryGetValue(key, out var el))
        {
            return defaultValue;
        }

        return el.TryGetDouble(out var value) ? value : defaultValue;
    }

    public static IReadOnlyList<string> GetStringArray(SessionEvent e, string key)
    {
        if (e.Extra is null || !e.Extra.TryGetValue(key, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<string>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var s = item.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    list.Add(s);
                }
            }
        }

        return list;
    }
}
