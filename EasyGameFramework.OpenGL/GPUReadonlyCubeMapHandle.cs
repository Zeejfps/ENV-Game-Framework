using EasyGameFramework.Api.Rendering;
using EasyGameFramework.OpenGL;

internal class GpuReadonlyCubeMapHandle : IGpuTextureHandle
{
    public int Width => m_Texture.Width;
    public int Height => m_Texture.Height;
    
    private readonly CubeMapTexture_GL m_Texture;

    public GpuReadonlyCubeMapHandle(CubeMapTexture_GL texture)
    {
        m_Texture = texture;
    }
}