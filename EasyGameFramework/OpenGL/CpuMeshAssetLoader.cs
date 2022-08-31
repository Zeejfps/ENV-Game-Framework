using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.AssetManagement;

public class CpuMeshAssetLoader : AssetLoader<ICpuMesh>
{
    protected override string FileExtension => ".mesh";

    protected override ICpuMesh Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        return CpuMesh.Deserialize(reader);
    }
}