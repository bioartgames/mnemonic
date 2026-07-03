using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mnemonic.Ipc;

/// <summary>
/// Godot JSON.stringify often emits whole numbers as floats (e.g. 25.0).
/// System.Text.Json rejects those for <see cref="int"/> unless coerced.
/// </summary>
internal sealed class FlexibleIntJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt32(out var value) => value,
            JsonTokenType.Number => Convert.ToInt32(reader.GetDouble()),
            JsonTokenType.String when int.TryParse(reader.GetString(), out var parsed) => parsed,
            _ => throw new JsonException($"Expected number for int, got {reader.TokenType}."),
        };
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}
