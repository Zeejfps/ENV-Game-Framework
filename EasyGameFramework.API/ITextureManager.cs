using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface ITextureManager
{
    void Bind(IHandle<IGpuTexture> handle);
}