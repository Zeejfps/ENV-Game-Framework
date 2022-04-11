using Framework.Assets;
using Newtonsoft.Json;

namespace Framework;

public class MaterialJsonAssetLoader : IAssetLoader<IMaterial>
{
    public IAsset LoadAsset(string assetPath)
    {
        var json = File.ReadAllText(assetPath);
        var materialAsset = JsonConvert.DeserializeObject<MaterialAssetJSON>(json);
        return new Material(materialAsset.Shader);
    }
}