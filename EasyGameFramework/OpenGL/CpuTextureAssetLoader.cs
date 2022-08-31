using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.OpenGL;

public class CpuTextureAssetLoader : AssetLoader<ICpuTexture>
{
    protected override string FileExtension => ".texture";

    protected override ICpuTexture Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var asset = CpuTexture.Deserialize(reader);
        return asset;
    }
}