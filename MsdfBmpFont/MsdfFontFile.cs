using System.Text.Json.Serialization;

namespace MsdfBmpFont;

public sealed class MsdfFontFile
{
    [JsonPropertyName("info")]
    public required FontInfo FontInfo { get; set; }

    [JsonPropertyName("common")]
    public required CommonInfo Common { get; set; }

    [JsonPropertyName("pages")]
    public required List<string> Pages { get; set; }

    [JsonPropertyName("chars")]
    public required List<GlyphInfo> Glyphs { get; set; }

    [JsonPropertyName("distanceField")]
    public required DistanceFieldInfo DistanceFieldInfo { get; set; }

    [JsonPropertyName("kernings")]
    public required List<KerningInfo> Kernings { get; set; }
}