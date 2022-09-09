using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal class TextureManager_GL : GpuResourceManager<IGpuTextureHandle, Texture2D_GL>, ITextureManager
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

    protected override IGpuTextureHandle CreateHandle(Texture2D_GL resource)
    {
        return new GpuReadonlyTextureHandle(resource);
    }

    protected override Texture2D_GL LoadAndBindResource(string assetPath)
    {
        var asset = m_CpuTextureAssetLoader.Load(assetPath);
        var width = asset.Width;
        var height = asset.Height;
        var pixels = asset.Pixels;
        var texture = ReadonlyTexture2D_GL.Create(width, height, pixels);
        return texture;
    }
}