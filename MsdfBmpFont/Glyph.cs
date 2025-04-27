using System.Text.Json.Serialization;

namespace MsdfBmpFont;

public sealed class Glyph
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("char")]
    public required string Char { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("xoffset")]
    public int XOffset { get; set; }

    [JsonPropertyName("yoffset")]
    public int YOffset { get; set; }

    [JsonPropertyName("xadvance")]
    public int XAdvance { get; set; }

    [JsonPropertyName("chnl")]
    public int Channel { get; set; }

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}