using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api.Rendering;

public interface IGpuTextureHandle : IHandle<IGpuTexture>
{
    public int Width { get; }
    public int Height { get; }
}