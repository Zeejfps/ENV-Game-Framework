using TicTacToePrototype.OpenGL.AssetLoaders;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class TextureFramebuffer_GL : IFramebuffer
{
    public int Width { get; }
    public int Height { get; }
    public ITexture? ColorTexture { get; }
    public ITexture? DepthTexture { get; }
    
    private uint m_Id;

    public TextureFramebuffer_GL(int width, int height)
    {
        Width = width;
        Height = height;

        m_Id = glGenFramebuffer();
        glBindFramebuffer(m_Id);

        var colorTexture = new Texture2D_GL(width, height);
        colorTexture.Use();
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, colorTexture.Id, 0);
        ColorTexture = colorTexture;

        var depthTexture = new Texture2D_GL(width, height);
        depthTexture.Use();
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, depthTexture.Id, 0);
        DepthTexture = depthTexture;
        
        glBindFramebuffer(0);
    }
    
    public void Use()
    {
        glBindFramebuffer(m_Id);
        glViewport(0, 0, Width, Height);
    }

    public void Clear()
    {
        glClearColor(1f, .607f, .82f, 1f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    }

    public void Dispose()
    {
    }
}