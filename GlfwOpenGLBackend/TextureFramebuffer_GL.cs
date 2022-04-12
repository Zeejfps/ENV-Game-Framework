using TicTacToePrototype.OpenGL.AssetLoaders;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class TextureFramebuffer_GL : IFramebuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public ITexture? ColorTexture => m_ColorTexture;
    public ITexture? DepthTexture => m_DepthTexture;

    private Texture2D_GL m_ColorTexture;
    private Texture2D_GL m_DepthTexture;
    private uint m_Id;

    public TextureFramebuffer_GL(int width, int height)
    {
        Width = width;
        Height = height;

        m_Id = glGenFramebuffer();
        glBindFramebuffer(m_Id);
        
        var colorTextureId = glGenTexture();
        glBindTexture(GL_TEXTURE_2D, colorTextureId);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, IntPtr.Zero);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR); 
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, colorTextureId, 0);
        m_ColorTexture = new Texture2D_GL(colorTextureId);

        var depthTextureId = glGenTexture();
        glBindTexture(GL_TEXTURE_2D, depthTextureId);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, IntPtr.Zero);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR); 
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, depthTextureId, 0);
        m_DepthTexture = new Texture2D_GL(depthTextureId);

        if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
            throw new Exception("Failed to create framebuffer");
        
        glBindFramebuffer(0);
    }
    
    public void Use()
    {
        glBindFramebuffer(m_Id);
        glViewport(0, 0, Width, Height);
        glEnable(GL_DEPTH_TEST);
    }

    public void Clear(float r, float g, float b)
    {
        glClearColor(r, g, b, 1f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    }

    public void Resize(int width, int height)
    {
        if (Width == width && Height == height)
            return;
        
        Width = width;
        Height = height;
        
        m_ColorTexture.Use();
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, IntPtr.Zero);
        
        m_DepthTexture.Use();
        glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, IntPtr.Zero);
    }

    public void Dispose()
    {
        m_ColorTexture.Unload();
        m_DepthTexture.Unload();
    }
}