using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal class GpuReadonlyTextureHandle : IHandle<IGpuTexture>
{
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