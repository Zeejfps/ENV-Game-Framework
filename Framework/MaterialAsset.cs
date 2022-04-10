using Newtonsoft.Json;

namespace ENV.Assets;

public class MaterialAsset
{
    [JsonProperty("shader")]
    public string Shader { get; set; }
}