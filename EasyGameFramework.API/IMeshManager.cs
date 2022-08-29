using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IMeshManager
{
    void Use(IHandle<IGpuMesh> handle);
    void Render();
    void RenderInstanced(int count);
}