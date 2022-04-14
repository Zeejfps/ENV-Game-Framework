using System.Diagnostics;
using GlfwOpenGLBackend.OpenGL;
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
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA16F, width, height, 0, GL_RGBA, GL_FLOAT, IntPtr.Zero);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR); 
            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0 + i, GL_TEXTURE_2D, colorTextureId, 0);
            m_ColorTextures[i] = new Texture2D_GL(colorTextureId);
            m_drawBufferIds[i] = GL_COLOR_ATTACHMENT0 + i;
        }
        
        glDrawBuffers(m_drawBufferIds);

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
    }

    public IFramebufferApi Use()
    {
        return Api.Use(this);
    }

    public void Dispose()
    {
        foreach (var colorTexture in m_ColorTextures)
            colorTexture.Unload();
        m_DepthTexture.Unload();
    }
    
    class Api : IFramebufferApi
    {
        private static Api? s_Instance;
        private static Api Instance => s_Instance ??= new Api();

        private TextureFramebuffer_GL? m_ActiveFramebuffer;
        
        public static Api Use(TextureFramebuffer_GL framebuffer)
        {
            Instance.m_ActiveFramebuffer = framebuffer;
            glBindFramebuffer(framebuffer.m_Id);
            glViewport(0, 0, framebuffer.Width, framebuffer.Height);
            return Instance;
        }
    
        public void Clear(float r, float g, float b, float a)
        {
            glClearColor(r, g, b, a);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        }

        public void Resize(int width, int height)
        {
            Debug.Assert(m_ActiveFramebuffer != null);
            if (m_ActiveFramebuffer.Width == width && m_ActiveFramebuffer.Height == height)
                return;
        
            m_ActiveFramebuffer.Width = width;
            m_ActiveFramebuffer.Height = height;

            foreach (var colorBuffer in m_ActiveFramebuffer.ColorBuffers)
            {
                colorBuffer.Use();
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA16F, width, height, 0, GL_RGBA, GL_FLOAT, IntPtr.Zero);   
            }

            m_ActiveFramebuffer.m_DepthTexture.Use();
            glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, IntPtr.Zero);
        }

        public void Dispose()
        {
        }
    }
}