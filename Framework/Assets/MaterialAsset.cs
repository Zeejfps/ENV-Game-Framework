namespace Framework.Assets;

public class MaterialAsset : IDisposable
{
    public string VertexShader { get; set; } = string.Empty;
    public string FragmentShader { get; set; } = string.Empty;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(VertexShader);
        writer.Write(FragmentShader);
    }

    public static MaterialAsset Deserialize(BinaryReader reader)
    {
        var vertexShader = reader.ReadString();
        var fragmentShader = reader.ReadString();

        return new MaterialAsset
        {
            VertexShader = vertexShader,
            FragmentShader = fragmentShader
        };
    }

    public void Dispose()
    {
        VertexShader = string.Empty;
        FragmentShader = string.Empty;
    }
}