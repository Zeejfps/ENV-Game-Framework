namespace Framework;

public interface IRenderbuffer : IFramebuffer, IDisposable
{
    ITexture ColorTexture { get; }
    ITexture? DepthTexture { get; }
}