namespace Framework.GLFW.NET;

public class TextureFramebuffer : IFramebuffer
{
    public int Width { get; }
    public int Height { get; }

    public TextureFramebuffer(int width, int height)
    {
        Width = width;
        Height = height;
    }
    
    public void Use()
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public void Resize(int width, int height)
    {
        throw new NotImplementedException();
    }
}