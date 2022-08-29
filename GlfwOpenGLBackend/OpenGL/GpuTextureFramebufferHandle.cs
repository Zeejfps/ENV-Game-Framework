using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class GpuTextureFramebufferHandle : IHandle<IGpuRenderbuffer>
{
    private readonly TextureFramebuffer_GL m_Framebuffer;

    public GpuTextureFramebufferHandle(TextureFramebuffer_GL framebuffer)
    {
        m_Framebuffer = framebuffer;
    }

    public IGpuRenderbuffer Use()
    {
        glBindFramebuffer(m_Framebuffer.Id);
        glViewport(0, 0, m_Framebuffer.Width, m_Framebuffer.Height);
        return m_Framebuffer;
    }
}