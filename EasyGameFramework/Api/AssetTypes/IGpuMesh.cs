namespace EasyGameFramework.Api.AssetTypes;

public interface IGpuMesh : IGpuAsset
{
    void Render();
    void RenderInstanced(int instanceCount);
}