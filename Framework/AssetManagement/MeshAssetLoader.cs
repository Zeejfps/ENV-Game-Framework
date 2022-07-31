using Framework.Assets;

namespace Framework;

public abstract class MeshAssetLoader : IAssetLoader<IGpuMesh>
{
    public IAsset LoadAsset(string assetPath)
    {
        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);

        using var meshAsset = MeshAsset.Deserialize(reader);
        var mesh = LoadAsset(meshAsset);
        return mesh;
    }

    protected abstract IGpuMesh LoadAsset(MeshAsset asset);
}