namespace Framework;

public interface IFramebuffer : IDisposable
{
    int Width { get; }
    int Height { get; }

    void Use();
    void Clear(float r, float g, float b);
    void Resize(int width, int height);
}