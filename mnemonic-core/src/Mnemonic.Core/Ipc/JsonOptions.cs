using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mnemonic.Ipc;

internal static class JsonOptions
{
    public static JsonSerializerOptions Shared { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
