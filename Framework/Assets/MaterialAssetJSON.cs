using Newtonsoft.Json;

namespace Framework.Assets;

public class MaterialAssetJSON
{
    [JsonProperty("shader")]
    public string Shader { get; set; }
}