namespace EasyGameFramework.API.AssetTypes;

public interface IGpuRenderbuffer : IGpuFramebuffer
{
    IGpuTexture[] ColorBuffers { get; }
    IGpuTexture? DepthBuffer { get; }
}