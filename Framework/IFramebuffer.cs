namespace Framework;

public interface IFramebuffer : IDisposable
{
    int Width { get; }
    int Height { get; }
    
    void Use();
    void Clear();
    void Resize(int width, int height);
}