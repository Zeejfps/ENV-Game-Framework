namespace Framework.Assets;

public class MaterialAsset_GL
{
    public string VertexShader { get; init; } = string.Empty;
    public string FragmentShader { get; init; } = string.Empty;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(VertexShader);
        writer.Write(FragmentShader);
    }

    public static MaterialAsset_GL Deserialize(BinaryReader reader)
    {
        var vertexShader = reader.ReadString();
        var fragmentShader = reader.ReadString();

        return new MaterialAsset_GL
        {
            VertexShader = vertexShader,
            FragmentShader = fragmentShader
        };
    }
}