using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IMeshManager
{
    void Bind(IHandle<IGpuMesh> handle);
    void Render();
    void RenderInstanced(int count);
}