namespace Framework.Assets;

public class MaterialAsset
{
    public string VertexShader { get; init; } = string.Empty;
    public string FragmentShader { get; init; } = string.Empty;

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
}