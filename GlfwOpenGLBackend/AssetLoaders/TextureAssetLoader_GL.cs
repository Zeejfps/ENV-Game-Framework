using Framework;
using TicTacToePrototype;
using TicTacToePrototype.OpenGL.AssetLoaders;

namespace GlfwOpenGLBackend.AssetLoaders;

public class TextureAssetLoader_GL : TextureAssetLoaderModule
{
    protected override ITexture LoadAsset(TextureAsset asset)
    {
        var width = asset.Width;
        var height = asset.Height;
        var texture = asset.Texture;
        var pixels = File.ReadAllBytes("Assets/Textures/" + texture + ".dds").Skip(148).ToArray();
        Console.WriteLine($"Read: {pixels.Length}");
        
        return new Texture2D_GL(width, height, pixels);
    }
}