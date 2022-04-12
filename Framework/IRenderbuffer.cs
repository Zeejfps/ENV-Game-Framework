namespace Framework;

public interface IRenderbuffer : IFramebuffer
{
    ITexture ColorTexture { get; }
    ITexture? DepthTexture { get; }
}