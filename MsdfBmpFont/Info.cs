using System.Text.Json.Serialization;

namespace MsdfBmpFont;

public class Info
{
    [JsonPropertyName("face")]
    public required string Face { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("bold")]
    public int Bold { get; set; }

    [JsonPropertyName("italic")]
    public int Italic { get; set; }

    [JsonPropertyName("charset")]
    public required List<string> Charset { get; set; }

    [JsonPropertyName("unicode")]
    public int Unicode { get; set; }

    [JsonPropertyName("stretchH")]
    public int StretchH { get; set; }

    [JsonPropertyName("smooth")]
    public int Smooth { get; set; }

    [JsonPropertyName("aa")]
    public int Aa { get; set; }

    [JsonPropertyName("padding")]
    public required List<int> Padding { get; set; }

    [JsonPropertyName("spacing")]
    public required List<int> Spacing { get; set; }
}