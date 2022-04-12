namespace Framework;

public interface IFramebuffer
{
    int Width { get; }
    int Height { get; }

    IFramebufferApi Use();
}