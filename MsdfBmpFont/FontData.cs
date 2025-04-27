using System.Text.Json.Serialization;

namespace MsdfBmpFontUtils;

public sealed class FontData
{
    [JsonPropertyName("info")]
    public Info Info { get; set; }

    [JsonPropertyName("common")]
    public Common Common { get; set; }

    [JsonPropertyName("pages")]
    public List<string> Pages { get; set; }

    [JsonPropertyName("chars")]
    public List<Glyph> Glyphs { get; set; }

    [JsonPropertyName("kernings")]
    public List<Kerning> Kernings { get; set; }
}