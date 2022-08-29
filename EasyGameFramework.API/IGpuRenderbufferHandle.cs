using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IGpuRenderbufferHandle : IHandle<IGpuRenderbuffer>
{
    public int Width { get; }
    public int Height { get; }
    
    public IHandle<IGpuTexture>[] ColorBuffers { get; }

    public IHandle<IGpuTexture>? DepthBuffer { get; }
}