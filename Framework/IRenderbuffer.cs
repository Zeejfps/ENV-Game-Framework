namespace Framework;

public interface IRenderbuffer : IFramebuffer, IDisposable
{
    ITexture[] ColorBuffers { get; }
    ITexture? DepthBuffer { get; }
}