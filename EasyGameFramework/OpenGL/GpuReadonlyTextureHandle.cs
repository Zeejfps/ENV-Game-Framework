using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal class GpuReadonlyTextureHandle : IGpuTextureHandle
{
    public int Width => m_Texture.Width;
    public int Height => m_Texture.Height;
    
    private readonly Texture2D_GL m_Texture;

    public GpuReadonlyTextureHandle(Texture2D_GL texture)
    {
        m_Texture = texture;
    }

    public IGpuTexture Use()
    {
        glBindTexture(GL_TEXTURE_2D, m_Texture.Id);
        return m_Texture;
    }
}