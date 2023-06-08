using EasyGameFramework.Api.Enums;

namespace EasyGameFramework.Api.Rendering;

public interface ITextureManager
{
    IGpuTextureHandle Load(string assetPath, TextureFilterKind filter = TextureFilterKind.Linear);
    void Bind(IGpuTextureHandle handle);
}