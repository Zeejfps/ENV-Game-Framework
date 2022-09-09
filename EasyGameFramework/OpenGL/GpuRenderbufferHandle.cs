using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.OpenGL;

internal class GpuRenderbufferHandle : IGpuRenderbufferHandle
{
    private readonly GpuRenderbuffer_GL m_Renderbuffer;

    public GpuRenderbufferHandle(GpuRenderbuffer_GL renderbuffer)
    {
        m_Renderbuffer = renderbuffer;
    }

    public int Width => m_Renderbuffer.Width;
    public int Height => m_Renderbuffer.Height;
    public bool HasDepthBuffer => DepthBuffer != null;

    public IGpuTextureHandle[] ColorBuffers => m_Renderbuffer.ColorBuffers;
    public IGpuTextureHandle? DepthBuffer => m_Renderbuffer.DepthBuffer;
}