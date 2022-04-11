using Framework.Assets;

namespace Framework;

public abstract class MaterialAssetLoader : IAssetLoader<IMaterial>
{
    public IAsset LoadAsset(string assetPath)
    {
        if (!File.Exists(assetPath))
            throw new Exception($"Failed to load material at path: {assetPath}");

        var fileExtension = Path.GetExtension(assetPath);
        if (fileExtension != ".material")
            throw new Exception("Unknown file type!");
        
        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);
        var materialAsset = MaterialAsset_GL.Deserialize(reader);
        return LoadAsset(materialAsset);
    }

    protected abstract IMaterial LoadAsset(MaterialAsset_GL asset);
}