using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IGpuFramebufferHandle : IHandle<IGpuFramebuffer>
{
    public int Width { get; }
    public int Height { get; }
}