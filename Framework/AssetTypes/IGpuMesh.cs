namespace Framework;

public interface IGpuMesh : IAsset
{
    IGpuMeshHandle Use();
}