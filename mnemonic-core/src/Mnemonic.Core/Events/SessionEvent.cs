using System.Text.Json;

namespace Mnemonic.Events;

public sealed class SessionEvent
{
    public required double T { get; init; }

    public required string Type { get; init; }

    public IReadOnlyDictionary<string, JsonElement>? Extra { get; init; }

    public static bool TryParseFromJsonLine(string line, out SessionEvent? evt)
    {
        evt = null;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!root.TryGetProperty("type", out var typeEl))
            {
                return false;
            }

            var type = typeEl.GetString();
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            double t;
            if (root.TryGetProperty("t", out var tEl) && tEl.TryGetDouble(out var parsedT))
            {
                t = parsedT;
            }
            else
            {
                t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            Dictionary<string, JsonElement>? extra = null;
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.NameEquals("t") || prop.NameEquals("type"))
                {
                    continue;
                }

                extra ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                extra[prop.Name] = prop.Value.Clone();
            }

            evt = new SessionEvent
            {
                T = t,
                Type = type,
                Extra = extra,
            };
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static SessionEvent Create(double t, string type) =>
        new() { T = t, Type = type };

    public static SessionEvent Create(double t, string type, IReadOnlyDictionary<string, object?> fields)
    {
        Dictionary<string, JsonElement>? extra = null;
        foreach (var pair in fields)
        {
            extra ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            extra[pair.Key] = JsonSerializer.SerializeToElement(pair.Value, (JsonSerializerOptions?)null);
        }

        return new SessionEvent
        {
            T = t,
            Type = type,
            Extra = extra,
        };
    }
}
