using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api;

public interface IMeshManager
{
    IHandle<IGpuMesh> Load(string assetPath);
    void Bind(IHandle<IGpuMesh> handle);
    void Render();
    void RenderInstanced(int count);
}