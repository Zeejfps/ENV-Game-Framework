namespace EasyGameFramework.API.AssetTypes;

public interface IGpuMesh : IGpuAsset
{
    void Render();
    void RenderInstanced(int instanceCount);
}