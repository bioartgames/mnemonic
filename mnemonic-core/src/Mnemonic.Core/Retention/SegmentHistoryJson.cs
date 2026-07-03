using System.Text.Json;

namespace Mnemonic.Retention;

public static class SegmentHistoryJson
{
    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static string ToJsonLine(SegmentHistoryRecord record) =>
        JsonSerializer.Serialize(record, CompactOptions);

    public static bool TryParseLine(string line, out SegmentHistoryRecord? record)
    {
        record = null;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<SegmentHistoryRecord>(line.TrimEnd('\r'), CompactOptions);
            if (parsed is null)
            {
                return false;
            }

            record = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
