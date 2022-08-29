using System.Diagnostics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using GlfwOpenGLBackend;
using GlfwOpenGLBackend.OpenGL;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class TextureFramebuffer_GL : IGpuRenderbuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public IHandle<IGpuTexture>[] ColorBuffers => m_ColorTextureHandles;
    public IHandle<IGpuTexture>? DepthBuffer => m_DepthTextureHandle;
    public uint Id => m_Id;

    private readonly IHandle<IGpuTexture>[] m_ColorTextureHandles;
    private IHandle<IGpuTexture> m_DepthTextureHandle;
    private uint m_Id;
    private int[] m_drawBufferIds;
    
    public TextureFramebuffer_GL(int width, int height, int colorBufferCount, bool createDepthBuffer)
    {
        Width = width;
        Height = height;

        m_Id = glGenFramebuffer();
        glBindFramebuffer(m_Id);
        
        m_ColorTextureHandles = new IHandle<IGpuTexture>[colorBufferCount];
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
    
    public void Clear(float r, float g, float b, float a)
    {
        glClearColor(r, g, b, a);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    }

    public void Resize(int width, int height)
    {
        if (Width == width && Height == height)
            return;
        
        Width = width;
        Height = height;

        foreach (var colorBuffer in ColorBuffers)
        {
            colorBuffer.Use();
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA16F, width, height, 0, GL_RGBA, GL_FLOAT, IntPtr.Zero);   
        }

        m_DepthTextureHandle.Use();
        glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, IntPtr.Zero);

    }

    public void Dispose()
    {
        
    }
}