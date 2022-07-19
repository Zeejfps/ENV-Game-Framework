using Framework.Assets;

namespace Framework;

public abstract class MaterialAssetLoader : IAssetLoader<IMaterial>
{
    private readonly Dictionary<string, IMaterial> m_PathToAssetMap = new Dictionary<string, IMaterial>();

    public IAsset LoadAsset(string assetPath)
    {
        if (m_PathToAssetMap.TryGetValue(assetPath, out var material))
            return material;
        
        if (!File.Exists(assetPath))
            throw new Exception($"Failed to load material at path: {assetPath}");

        // var fileExtension = Path.GetExtension(assetPath);
        // if (fileExtension != ".material")
        //     throw new Exception("Unknown file type!");
        
        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);
        var materialAsset = MaterialAsset.Deserialize(reader);

        material = LoadAsset(materialAsset);
        m_PathToAssetMap[assetPath] = material;
        return material;
    }

    protected abstract IMaterial LoadAsset(MaterialAsset asset);
}