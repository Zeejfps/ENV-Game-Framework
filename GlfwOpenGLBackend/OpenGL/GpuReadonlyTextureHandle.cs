using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using GlfwOpenGLBackend.OpenGL;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class GpuReadonlyTextureHandle : IHandle<IGpuTexture>
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