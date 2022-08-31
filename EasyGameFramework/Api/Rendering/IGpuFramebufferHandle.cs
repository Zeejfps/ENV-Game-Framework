using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api.Rendering;

public interface IGpuFramebufferHandle : IHandle<IGpuFramebuffer>
{
    public int Width { get; }
    public int Height { get; }
}