using System.Text.Json.Serialization;

namespace MsdfBmpFont;

public sealed class DistanceFieldInfo
{
    [JsonPropertyName("fieldType")]
    public string FieldType { get; set; }

    [JsonPropertyName("distanceRange")]
    public int DistanceRange { get; set; }
}