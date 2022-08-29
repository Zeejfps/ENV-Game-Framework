using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Framework.GLFW.NET;

public class GpuWindowFramebufferHandle : IGpuFramebufferHandle
{
    public int Width => m_Framebuffer.Width;
    public int Height => m_Framebuffer.Height;
    
    private readonly IGpuFramebuffer m_Framebuffer;

    public GpuWindowFramebufferHandle(IGpuFramebuffer framebuffer)
    {
        m_Framebuffer = framebuffer;
    }
}