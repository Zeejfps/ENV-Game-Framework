using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using EasyGameFramework.OpenGL;
using static OpenGL.Gl;

internal class GpuReadonlyCubeMapHandle : IGpuTextureHandle
{
    public int Width => m_Texture.Width;
    public int Height => m_Texture.Height;
    
    private readonly CubeMapTexture_GL m_Texture;

    public GpuReadonlyCubeMapHandle(CubeMapTexture_GL texture)
    {
        m_Texture = texture;
    }

    public IGpuTexture Use()
    {
        glBindTexture(GL_TEXTURE_CUBE_MAP, m_Texture.Id);
        return m_Texture;
    }
}