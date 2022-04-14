using Framework;
using TicTacToePrototype;
using TicTacToePrototype.OpenGL.AssetLoaders;

namespace GlfwOpenGLBackend.AssetLoaders;

public class TextureAssetLoader_GL : TextureAssetLoaderModule
{
    protected override ITexture LoadAsset(TextureAsset_GL asset)
    {
        var width = asset.Width;
        var height = asset.Height;
        var pixels = asset.Pixels;
        return new ReadonlyTexture2D_GL(width, height, pixels);
    }
}