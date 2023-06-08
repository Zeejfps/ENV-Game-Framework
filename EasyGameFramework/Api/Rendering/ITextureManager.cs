using EasyGameFramework.Api.Enums;

namespace EasyGameFramework.Api.Rendering;

public interface ITextureManager
{
    IGpuTextureHandle Load(string assetPath, TextureFilterKind filter);
    void Bind(IGpuTextureHandle handle);
}