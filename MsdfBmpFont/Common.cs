using System.Text.Json.Serialization;

namespace MsdfBmpFontUtils;

public class Common
{
    [JsonPropertyName("lineHeight")]
    public int LineHeight { get; set; }

    [JsonPropertyName("base")]
    public int Base { get; set; }

    [JsonPropertyName("scaleW")]
    public int ScaleW { get; set; }

    [JsonPropertyName("scaleH")]
    public int ScaleH { get; set; }

    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("packed")]
    public int Packed { get; set; }

    [JsonPropertyName("alphaChnl")]
    public int AlphaChnl { get; set; }

    [JsonPropertyName("redChnl")]
    public int RedChnl { get; set; }

    [JsonPropertyName("greenChnl")]
    public int GreenChnl { get; set; }

    [JsonPropertyName("blueChnl")]
    public int BlueChnl { get; set; }
}