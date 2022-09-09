using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.Api.AssetTypes;

public interface IGpuRenderbuffer : IGpuFramebuffer
{
    IGpuTextureHandle[] ColorBuffers { get; }
    IGpuTextureHandle? DepthBuffer { get; }
}