using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.AssetManagement;
using Framework;
using TicTacToePrototype;
using TicTacToePrototype.OpenGL.AssetLoaders;

namespace GlfwOpenGLBackend.AssetLoaders;

public class GpuTextureAssetLoader_GL : IAssetLoader<IGpuTexture>
{
    private readonly IAssetLoader<ICpuTexture> m_CpuTextureAssetLoader = new CpuTextureAssetLoader();

    public IGpuTexture Load(string assetPath)
    {
        var asset = m_CpuTextureAssetLoader.Load(assetPath);
        var width = asset.Width;
        var height = asset.Height;
        var pixels = asset.Pixels;
        return new ReadonlyTexture2D_GL(width, height, pixels);
    }
}