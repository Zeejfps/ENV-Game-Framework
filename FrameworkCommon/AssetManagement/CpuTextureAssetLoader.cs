using TicTacToePrototype;

namespace Framework;

public class CpuTextureAssetLoader : AssetLoader<ICpuTexture>
{
    protected override ICpuTexture Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var asset = CpuTexture.Deserialize(reader);
        return asset;
    }
}