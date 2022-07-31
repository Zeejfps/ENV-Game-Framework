using Framework.Assets;

namespace Framework;

public abstract class MaterialAssetLoader : IAssetLoader<IGpuShader>
{
    private readonly Dictionary<string, IGpuShader> m_PathToAssetMap = new Dictionary<string, IGpuShader>();

    public IAsset LoadAsset(string assetPath)
    {
        if (m_PathToAssetMap.TryGetValue(assetPath, out var material))
            return material;
        
        if (!File.Exists(assetPath))
            throw new Exception($"File does not exists at path: {assetPath}");

        // var fileExtension = Path.GetExtension(assetPath);
        // if (fileExtension != ".material")
        //     throw new Exception("Unknown file type!");
        
        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);

        try
        {
            var materialAsset = CpuShader.Deserialize(reader);
            material = LoadAsset(materialAsset);
            m_PathToAssetMap[assetPath] = material;
            return material;
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to deserialize asset: {assetPath}");
        }
    }

    protected abstract IGpuShader LoadAsset(CpuShader asset);
}