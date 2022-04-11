namespace Framework;

public class MeshObjAssetLoaderModule : IAssetLoader<IMesh>
{
    private readonly Dictionary<string, IMesh> m_LoadedAssets = new();

    public IAsset LoadAsset(string assetPath)
    {
        var fileExtension = Path.GetExtension(assetPath);
        if (fileExtension != ".obj")
            throw new Exception($"Invalid Asset Extension: {fileExtension}");

        if (m_LoadedAssets.TryGetValue(assetPath, out var asset) && asset.IsLoaded)
            return asset;

        asset = OBJLoader.LoadObjFromFile(assetPath);
        m_LoadedAssets[assetPath] = asset;
        return asset;
    }
}