using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api.Rendering;

public interface ITextureManager
{
    IGpuTextureHandle Load(string assetPath);
    void Bind(IGpuTextureHandle handle);
}