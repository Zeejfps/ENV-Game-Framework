using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api;

public interface IViewport
{
    float Left { get; }
    float Top { get; }
    float Right { get; }
    float Bottom { get; }
    float AspectRatio { get; }
    IHandle<IGpuRenderbuffer> Framebuffer { get; }
}