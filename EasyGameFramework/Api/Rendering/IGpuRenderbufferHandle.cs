using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api.Rendering;

public interface IGpuRenderbufferHandle : IHandle<IGpuRenderbuffer>
{
    public int Width { get; }
    public int Height { get; }
    bool HasDepthBuffer { get; }

    public IGpuTextureHandle[] ColorBuffers { get; }
    public IGpuTextureHandle? DepthBuffer { get; }
}