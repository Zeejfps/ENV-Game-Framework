using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface ITextureManager
{
    void Use(IHandle<IGpuTexture> handle);
}