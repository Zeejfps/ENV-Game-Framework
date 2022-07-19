namespace Framework;

public interface IFramebufferHandle : IDisposable
{
    void Clear(float r, float g, float b, float a);
    void Resize(int width, int height);
}