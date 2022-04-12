using TicTacToePrototype.OpenGL.AssetLoaders;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class TextureFramebuffer_GL : IFramebuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public ITexture? ColorTexture => m_ColorTexture;
    public ITexture? DepthTexture => m_DepthTexture;

    private ResizableTexture2D_GL m_ColorTexture;
    private ResizableTexture2D_GL m_DepthTexture;
    private uint m_Id;

    public TextureFramebuffer_GL(int width, int height)
    {
        Width = width;
        Height = height;

        m_Id = glGenFramebuffer();
        glBindFramebuffer(m_Id);

        m_ColorTexture = new ResizableTexture2D_GL(width, height);
        m_ColorTexture.Use();
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, m_ColorTexture.Id, 0);

        m_DepthTexture = new ResizableTexture2D_GL(width, height);
        m_DepthTexture.Use();
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, m_DepthTexture.Id, 0);
        
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

    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
        m_ColorTexture.Resize(width, height);
        m_DepthTexture.Resize(width, height);
    }

    public void Dispose()
    {
        m_ColorTexture.Unload();
        m_DepthTexture.Unload();
    }
}