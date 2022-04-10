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

public abstract class MaterialAssetLoader : IAssetLoader<IMaterial>
{
    public IAsset LoadAsset(string assetPath)
    {
        if (!File.Exists(assetPath))
            throw new Exception($"Failed to load material at path: {assetPath}");

        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);
        var materialAsset = MaterialAsset.Deserialize(reader);
        return LoadAsset(materialAsset);
    }

    protected abstract IMaterial LoadAsset(MaterialAsset asset);
}