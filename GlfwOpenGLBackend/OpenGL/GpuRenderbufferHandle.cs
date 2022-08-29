using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Framework.GLFW.NET;

public class GpuRenderbufferHandle : IGpuRenderbufferHandle
{
    public int Width => m_Renderbuffer.Width;
    public int Height => m_Renderbuffer.Height;
    
    public IHandle<IGpuTexture>[] ColorBuffers => m_Renderbuffer.ColorBuffers;
    public IHandle<IGpuTexture>? DepthBuffer => m_Renderbuffer.DepthBuffer;
    
    private readonly GpuRenderbuffer_GL m_Renderbuffer;

    public GpuRenderbufferHandle(GpuRenderbuffer_GL renderbuffer)
    {
        m_Renderbuffer = renderbuffer;
    }
}