using TicTacToePrototype;

namespace Framework;

public abstract class TextureAssetLoader : IAssetLoader<ITexture>
{
    public IAsset LoadAsset(string assetPath)
    {
        if (!File.Exists(assetPath))
            throw new Exception($"File does not exists {assetPath}");

        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);

        var asset = TextureAsset.Deserialize(reader);
        var texture = LoadAsset(asset);
        return texture;
    }

    protected abstract ITexture LoadAsset(TextureAsset asset);
}