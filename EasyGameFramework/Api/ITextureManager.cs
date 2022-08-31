using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api;

public interface ITextureManager
{
    IHandle<IGpuTexture> Load(string assetPath);
    void Bind(IHandle<IGpuTexture> handle);
}