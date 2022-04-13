using TicTacToePrototype.OpenGL.AssetLoaders;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class TextureFramebuffer_GL : IRenderbuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public ITexture[] ColorBuffers => m_ColorTextures;
    public ITexture? DepthBuffer => m_DepthTexture;

    private readonly Texture2D_GL[] m_ColorTextures;
    private Texture2D_GL m_DepthTexture;
    private uint m_Id;
    private int[] m_drawBufferIds;

    private Api m_Api;

    public TextureFramebuffer_GL(int width, int height, int colorBufferCount, bool createDepthBuffer)
    {
        Width = width;
        Height = height;

        m_Id = glGenFramebuffer();
        glBindFramebuffer(m_Id);
        
        m_ColorTextures = new Texture2D_GL[colorBufferCount];
        m_drawBufferIds = new int[colorBufferCount];
        for (var i = 0; i < colorBufferCount; i++)
        {
            var colorTextureId = glGenTexture();
            glBindTexture(GL_TEXTURE_2D, colorTextureId);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB16F, width, height, 0, GL_RGB, GL_FLOAT, IntPtr.Zero);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR); 
            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0 + i, GL_TEXTURE_2D, colorTextureId, 0);
            m_ColorTextures[i] = new Texture2D_GL(colorTextureId);
            m_drawBufferIds[i] = GL_COLOR_ATTACHMENT0 + i;
        }
        
        if (createDepthBuffer)
        {
            var depthTextureId = glGenTexture();
            glBindTexture(GL_TEXTURE_2D, depthTextureId);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, IntPtr.Zero);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR); 
            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, depthTextureId, 0);
            m_DepthTexture = new Texture2D_GL(depthTextureId);
        }

        if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
            throw new Exception("Failed to create framebuffer");
        
        glBindFramebuffer(0);

        m_Api = new Api(this);
    }

    public IFramebufferApi Use()
    {
        m_Api.Use();
        return m_Api;
    }

    public void Dispose()
    {
        foreach (var colorTexture in m_ColorTextures)
            colorTexture.Unload();
        m_DepthTexture.Unload();
    }
    
    class Api : IFramebufferApi
    {
        private readonly TextureFramebuffer_GL m_Framebuffer;
    
        public Api(TextureFramebuffer_GL framebuffer)
        {
            m_Framebuffer = framebuffer;
        }

        public void Use()
        {
            glBindFramebuffer(m_Framebuffer.m_Id);
            glDrawBuffers(m_Framebuffer.m_drawBufferIds);
            glViewport(0, 0, m_Framebuffer.Width, m_Framebuffer.Height);
        }
    
        public void Clear(float r, float g, float b)
        {
            glClearColor(r, g, b, 1f);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        }

        public void Resize(int width, int height)
        {
            if (m_Framebuffer.Width == width && m_Framebuffer.Height == height)
                return;
        
            m_Framebuffer.Width = width;
            m_Framebuffer.Height = height;

            foreach (var colorBuffer in m_Framebuffer.ColorBuffers)
            {
                colorBuffer.Use();
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, IntPtr.Zero);   
            }

            m_Framebuffer.m_DepthTexture.Use();
            glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, IntPtr.Zero);
        }

        public void Dispose()
        {
            glBindFramebuffer(0);
        }
    }
}