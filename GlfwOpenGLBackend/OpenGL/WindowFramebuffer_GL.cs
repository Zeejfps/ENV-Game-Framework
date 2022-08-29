using EasyGameFramework.API.AssetTypes;
using OpenGL;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class WindowFramebuffer_GL : IGpuFramebuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    private Handle m_Handle;

    public WindowFramebuffer_GL(int width, int height, GetProcAddressHandler getProcAddress)
    {
        m_Handle = new Handle(this);
        Width = width;
        Height = height;
        Import(getProcAddress);
    }

    public IGpuFramebufferHandle Use()
    {
        m_Handle.Use();
        return m_Handle;
    }
    
    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
    }
    
    class Handle : IGpuFramebufferHandle
    {
        private readonly WindowFramebuffer_GL m_Framebuffer;
    
        public Handle(WindowFramebuffer_GL framebuffer)
        {
            m_Framebuffer = framebuffer;
        }

        public void Use()
        {
            glBindFramebuffer(0);
            glViewport(0, 0, m_Framebuffer.Width, m_Framebuffer.Height);
        }
    
        public void Clear(float r, float g, float b, float a)
        {
            glClearColor(r, g, b, a);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        }

        public void Resize(int width, int height)
        {
            m_Framebuffer.Width = width;
            m_Framebuffer.Height = height;
            glViewport(0, 0, width, height);
        }

        public void Dispose()
        {
            glBindFramebuffer(0);
        }
    }
}