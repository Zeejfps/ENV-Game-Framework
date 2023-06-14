using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Core;

namespace EasyGameFramework.Api.Rendering;

public interface IMeshController
{
    IHandle<IGpuMesh> Load(string assetPath);
    IHandle<IGpuMesh> CreateAndBind(CpuMesh mesh);
    void Bind(IHandle<IGpuMesh> handle);
    void Render();
    void RenderInstanced(int count);
}