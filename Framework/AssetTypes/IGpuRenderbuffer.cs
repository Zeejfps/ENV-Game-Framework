namespace Framework;

public interface IGpuRenderbuffer : IGpuFramebuffer
{
    IGpuTexture[] ColorBuffers { get; }
    IGpuTexture? DepthBuffer { get; }
}