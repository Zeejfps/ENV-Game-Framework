using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;

namespace Framework.GLFW.NET;

public class GpuWindowFramebufferHandle : IGpuFramebufferHandle
{
    private readonly IGpuFramebuffer m_Framebuffer;

    public GpuWindowFramebufferHandle(IGpuFramebuffer framebuffer)
    {
        m_Framebuffer = framebuffer;
    }

    public int Width => m_Framebuffer.Width;
    public int Height => m_Framebuffer.Height;
}