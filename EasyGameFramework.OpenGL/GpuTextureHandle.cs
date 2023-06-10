using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

public class GpuTextureHandle : IGpuTextureHandle
{
    public int Width => m_Texture.Width;
    public int Height => m_Texture.Height;
    
    private readonly Texture2D_GL m_Texture;

    public GpuTextureHandle(Texture2D_GL texture)
    {
        m_Texture = texture;
    }
}