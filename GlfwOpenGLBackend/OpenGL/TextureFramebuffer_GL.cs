using System.Diagnostics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using GlfwOpenGLBackend;
using GlfwOpenGLBackend.OpenGL;
using TicTacToePrototype.OpenGL.AssetLoaders;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class TextureFramebuffer_GL : IGpuRenderbuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public IHandle<IGpuTexture>[] ColorBuffers => m_ColorTextureHandles;
    public IHandle<IGpuTexture>? DepthBuffer => m_DepthTextureHandle;

    private readonly GpuTextureHandle[] m_ColorTextureHandles;
    private GpuTextureHandle m_DepthTextureHandle;
    private uint m_Id;
    private int[] m_drawBufferIds;
    
    public TextureFramebuffer_GL(int width, int height, int colorBufferCount, bool createDepthBuffer)
    {
        Width = width;
        Height = height;

        m_Id = glGenFramebuffer();
        glBindFramebuffer(m_Id);
        
        m_ColorTextureHandles = new GpuTextureHandle[colorBufferCount];
        m_drawBufferIds = new int[colorBufferCount];
        for (var i = 0; i < colorBufferCount; i++)
        {
            var colorTextureId = glGenTexture();
            glBindTexture(GL_TEXTURE_2D, colorTextureId);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA16F, width, height, 0, GL_RGBA, GL_FLOAT, IntPtr.Zero);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR); 
            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0 + i, GL_TEXTURE_2D, colorTextureId, 0);
            m_ColorTextureHandles[i] = new GpuTextureHandle(new Texture2D_GL(colorTextureId));
            m_drawBufferIds[i] = GL_COLOR_ATTACHMENT0 + i;
        }
        
        if (m_drawBufferIds.Length > 0)
            glDrawBuffers(m_drawBufferIds);

        if (createDepthBuffer)
        {
            var depthTextureId = glGenTexture();
            glBindTexture(GL_TEXTURE_2D, depthTextureId);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH24_STENCIL8, width, height, 0, GL_DEPTH_STENCIL, GL_UNSIGNED_INT_24_8, IntPtr.Zero);
            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT , GL_TEXTURE_2D, depthTextureId, 0);
            m_DepthTextureHandle = new GpuTextureHandle(new Texture2D_GL(depthTextureId));
        }
        
        if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
            throw new Exception("Failed to create framebuffer");
        
        glBindFramebuffer(0);
    }

    public IGpuFramebufferHandle Use()
    {
        return Handle.Use(this);
    }

    public void Dispose()
    {
        
    }
    
    class Handle : IGpuFramebufferHandle
    {
        private static Handle? s_Instance;
        private static Handle Instance => s_Instance ??= new Handle();

        private TextureFramebuffer_GL? m_ActiveFramebuffer;
        
        public static Handle Use(TextureFramebuffer_GL framebuffer)
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

            m_ActiveFramebuffer.m_DepthTextureHandle.Use();
            glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, IntPtr.Zero);
        }

        public void Dispose()
        {
        }
    }
}