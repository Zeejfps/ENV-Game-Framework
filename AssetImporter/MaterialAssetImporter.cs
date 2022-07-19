using Framework.Assets;

namespace AssetImporter;

public class MaterialAssetImporter
{
    public string VertexShaderSource { get; set; }
    public string FragmentShaderSource { get; set; }

    public void Import(string outputPath)
    {
        var materialAsset = new MaterialAsset
        {
            VertexShader = VertexShaderSource,
            FragmentShader = FragmentShaderSource
        };
        
        using var stream = File.Open(Path.GetFullPath(outputPath), FileMode.Create);
        using var writer = new BinaryWriter(stream);
        materialAsset.Serialize(writer);
    }
}