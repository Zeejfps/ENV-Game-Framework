using OpenGL;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class WindowFramebuffer_GL : IFramebuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    private Api m_Api;

    public WindowFramebuffer_GL()
    {
        m_Api = new Api(this);
    }
    
    public void Init(int width, int height, GetProcAddressHandler getProcAddress)
    {
        Width = width;
        Height = height;
        Import(getProcAddress);
        
        //glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    }

    public IFramebufferApi Use()
    {
        m_Api.Use();
        return m_Api;
    }
    
    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
    }
    
    class Api : IFramebufferApi
    {
        private readonly WindowFramebuffer_GL m_Framebuffer;
    
        public Api(WindowFramebuffer_GL framebuffer)
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