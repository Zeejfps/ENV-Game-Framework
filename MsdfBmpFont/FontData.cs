using System.Text.Json.Serialization;

namespace MsdfBmpFont;

public sealed class FontData
{
    [JsonPropertyName("info")]
    public required Info Info { get; set; }

    [JsonPropertyName("common")]
    public required Common Common { get; set; }

    [JsonPropertyName("pages")]
    public required List<string> Pages { get; set; }

    [JsonPropertyName("chars")]
    public required List<Glyph> Glyphs { get; set; }

    [JsonPropertyName("distanceField")]
    public required DistanceFieldInfo DistanceFieldInfo { get; set; }

    [JsonPropertyName("kernings")]
    public List<Kerning>? Kernings { get; set; }
}