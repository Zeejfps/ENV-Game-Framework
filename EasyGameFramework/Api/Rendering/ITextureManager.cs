using EasyGameFramework.Api.Enums;

namespace EasyGameFramework.Api.Rendering;

public interface ITextureManager
{
    IGpuTextureHandle Load(string assetPath);
    void Bind(IGpuTextureHandle handle);
    TextureFilterKind Filter { get; set; }
}