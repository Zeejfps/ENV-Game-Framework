using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using TicTacToePrototype.OpenGL.AssetLoaders;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class GpuReadonlyTextureHandle : IHandle<IGpuTexture>
{
    private readonly ReadonlyTexture2D_GL m_Texture;

    public GpuReadonlyTextureHandle(ReadonlyTexture2D_GL texture)
    {
        m_Texture = texture;
    }

    public IGpuTexture Use()
    {
        glBindTexture(GL_TEXTURE_2D, m_Texture.Id);
        return m_Texture;
    }
}