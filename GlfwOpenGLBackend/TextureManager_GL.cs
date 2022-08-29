using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using GlfwOpenGLBackend.OpenGL;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class TextureManager_GL : GpuResourceManager<IHandle<IGpuTexture>, Texture2D_GL>, ITextureManager
{
    protected override void OnBound(Texture2D_GL resource)
    {
        glBindTexture(GL_TEXTURE_2D, resource.Id);
    }

    protected override void OnUnbound()
    {
        glBindTexture(GL_TEXTURE_2D, 0);
    }
}