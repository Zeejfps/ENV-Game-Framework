namespace EasyGameFramework.API.AssetTypes;

public interface IGpuMesh : IAsset
{
    IGpuMeshHandle Use();
}