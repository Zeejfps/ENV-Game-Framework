namespace EasyGameFramework.API.AssetTypes;

public interface IGpuMeshHandle : IDisposable
{
    void Render();
    void RenderInstanced(int instanceCount);
}