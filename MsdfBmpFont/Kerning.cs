using System.Text.Json.Serialization;

namespace MsdfBmpFontUtils;

public sealed class Kerning
{
    [JsonPropertyName("first")]
    public int First { get; set; }

    [JsonPropertyName("second")]
    public int Second { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}