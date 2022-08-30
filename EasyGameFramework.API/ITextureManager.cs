using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface ITextureManager
{
    IHandle<IGpuTexture> Load(string assetPath);
    void Bind(IHandle<IGpuTexture> handle);
}