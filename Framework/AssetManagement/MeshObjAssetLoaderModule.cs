using Framework.Assets;

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

public class MeshAssetLoader : IAssetLoader<IMesh>
{
    public IAsset LoadAsset(string assetPath)
    {
        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);

        var meshAsset = MeshAsset_GL.Deserialize(reader);

        return new Mesh
        {
            Vertices = meshAsset.Vertices,
            Normals = meshAsset.Normals,
            Triangles = meshAsset.Triangles,
            Uvs = meshAsset.Uvs,
            Tangents = meshAsset.Tangents,
        };
    }
}
