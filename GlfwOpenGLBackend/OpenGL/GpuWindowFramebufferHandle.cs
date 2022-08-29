using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using OpenGL;

namespace Framework.GLFW.NET;

public class GpuWindowFramebufferHandle : IHandle<IGpuFramebuffer>
{
    private readonly WindowFramebuffer_GL m_Framebuffer;

    public GpuWindowFramebufferHandle(WindowFramebuffer_GL framebuffer)
    {
        m_Framebuffer = framebuffer;
    }

    public IGpuFramebuffer Use()
    {
        Gl.glBindFramebuffer(m_Framebuffer.Id);
        Gl.glViewport(0, 0, m_Framebuffer.Width, m_Framebuffer.Height);
        return m_Framebuffer;
    }
}