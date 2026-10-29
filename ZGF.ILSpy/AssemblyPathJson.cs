using System.Text.Json;
using System.Text.Json.Serialization;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>Writes an <see cref="AssemblyPath"/> as its text, as a value or as a key, and reads one back,
/// refusing text that is no absolute path.</summary>
internal sealed class AssemblyPathJson : JsonConverter<AssemblyPath>
{
    public override AssemblyPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, AssemblyPath value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Text);

    public override AssemblyPath ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Parse(reader.GetString());

    public override void WriteAsPropertyName(Utf8JsonWriter writer, AssemblyPath value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value.Text);

    private static AssemblyPath Parse(string? text)
    {
        if (text == null)
            throw new JsonException("An assembly path is missing.");
        var path = AssemblyPath.Parse(text);
        if (path == null)
            throw new JsonException($"'{text}' is no absolute path.");
        return path;
    }
}
