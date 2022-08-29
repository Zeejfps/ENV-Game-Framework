using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IRenderer
{
    void Render(IMaterial material, IHandle<IGpuMesh> meshHandle);
}