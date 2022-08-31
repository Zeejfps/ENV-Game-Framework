using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api;

public interface IGpuRenderbufferHandle : IHandle<IGpuRenderbuffer>
{
    public int Width { get; }
    public int Height { get; }
    bool HasDepthBuffer { get; }

    public IHandle<IGpuTexture>[] ColorBuffers { get; }
    public IHandle<IGpuTexture>? DepthBuffer { get; }
}