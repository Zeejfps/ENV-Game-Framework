using Newtonsoft.Json;

namespace ENV.Assets;

public class MaterialAssetJSON
{
    [JsonProperty("shader")]
    public string Shader { get; set; }
}