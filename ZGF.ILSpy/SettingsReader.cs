using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ZGF.ILSpy;

/// <summary>Reads the fields of a settings file one at a time, so that a value which is not what its
/// field holds leaves out that field alone, for its default to stand in, and is named in
/// <see cref="LeftOut"/>.</summary>
internal sealed class SettingsReader
{
    private readonly Dictionary<string, JsonElement> _fields;
    private readonly List<string> _leftOut = new();

    public SettingsReader(Dictionary<string, JsonElement> fields)
    {
        _fields = fields;
    }

    /// <summary>The fields left out, in the order they were read, each once.</summary>
    public IReadOnlyList<string> LeftOut => _leftOut;

    /// <summary>What <paramref name="field"/> holds, or the default when the file lacks it, holds
    /// null, or holds something else.</summary>
    public T? Read<T>(string field, JsonTypeInfo<T> type)
    {
        if (!_fields.TryGetValue(field, out var value))
            return default;
        return Parse(value, type, field);
    }

    /// <summary>The elements of the array <paramref name="field"/> holds, for each to be read on its
    /// own; none when the file lacks it, holds null, or holds something else.</summary>
    public List<JsonElement> ReadEach(string field)
    {
        return Read(field, AppSettingsJson.Default.ListJsonElement) ?? new List<JsonElement>();
    }

    /// <summary><paramref name="value"/>, a part of <paramref name="field"/>, as <paramref name="type"/>;
    /// the default, with the field left out, when it is something else.</summary>
    public T? Parse<T>(JsonElement value, JsonTypeInfo<T> type, string field)
    {
        try
        {
            return value.Deserialize(type);
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            LeaveOut(field);
            return default;
        }
    }

    public void LeaveOut(string field)
    {
        if (!_leftOut.Contains(field))
            _leftOut.Add(field);
    }
}
