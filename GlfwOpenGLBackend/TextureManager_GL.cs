using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.AssetManagement;
using GlfwOpenGLBackend.OpenGL;
using TicTacToePrototype.OpenGL.AssetLoaders;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class TextureManager_GL : GpuResourceManager<IHandle<IGpuTexture>, Texture2D_GL>, ITextureManager
{
    private readonly CpuTextureAssetLoader m_CpuTextureAssetLoader = new();

    protected override void OnBound(Texture2D_GL resource)
    {
        glBindTexture(GL_TEXTURE_2D, resource.Id);
    }

    protected override void OnUnbound()
    {
        glBindTexture(GL_TEXTURE_2D, 0);
    }

    public IHandle<IGpuTexture> Load(string assetPath)
    {
        var asset = m_CpuTextureAssetLoader.Load(assetPath);
        var width = asset.Width;
        var height = asset.Height;
        var pixels = asset.Pixels;
        var texture = ReadonlyTexture2D_GL.Create(width, height, pixels);
        var handle = new GpuReadonlyTextureHandle(texture);
        Add(handle, texture);
        BoundResource = texture;
        return handle;
    }
}