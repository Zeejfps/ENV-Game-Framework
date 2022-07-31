namespace Framework;

public interface IRenderbuffer : IGpuFramebuffer, IDisposable
{
    IGpuTexture[] ColorBuffers { get; }
    IGpuTexture? DepthBuffer { get; }
}