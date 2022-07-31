using Framework.Assets;

namespace Framework;

public class CpuShaderAssetLoader : AssetLoader<ICpuShader>
{
    protected override ICpuShader Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var shader = CpuShader.Deserialize(reader);
        return shader;
    }
}