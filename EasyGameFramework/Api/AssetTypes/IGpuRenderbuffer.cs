namespace EasyGameFramework.Api.AssetTypes;

public interface IGpuRenderbuffer : IGpuFramebuffer
{
    IHandle<IGpuTexture>[] ColorBuffers { get; }
    IHandle<IGpuTexture>? DepthBuffer { get; }
}