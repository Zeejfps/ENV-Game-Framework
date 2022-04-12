namespace Framework;

public interface IFramebufferApi : IDisposable
{
    void Clear(float r, float g, float b);
    void Resize(int width, int height);
}