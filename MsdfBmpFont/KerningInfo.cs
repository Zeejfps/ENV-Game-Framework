using System.Text.Json.Serialization;

namespace MsdfBmpFont;

public sealed class KerningInfo
{
    [JsonPropertyName("first")]
    public int First { get; set; }

    [JsonPropertyName("second")]
    public int Second { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}