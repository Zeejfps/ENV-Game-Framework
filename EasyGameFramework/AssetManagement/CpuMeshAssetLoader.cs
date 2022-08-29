using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.AssetManagement;

public class CpuMeshAssetLoader : AssetLoader<ICpuMesh>
{
    protected override ICpuMesh Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        return CpuMesh.Deserialize(reader);
    }
}