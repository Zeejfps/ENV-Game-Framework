namespace Framework.Assets;

public class MaterialAsset
{
    public byte[] VertexShader { get; init; } = Array.Empty<byte>();
    public byte[] FragmentShader { get; init; } = Array.Empty<byte>();

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(VertexShader.Length);
        writer.Write(VertexShader);
        writer.Write(FragmentShader.Length);
        writer.Write(FragmentShader);
    }

    public static MaterialAsset Deserialize(BinaryReader reader)
    {
        var vertexShaderLength = reader.ReadInt32();
        var vertexShader = reader.ReadBytes(vertexShaderLength);

        var fragmentShaderLength = reader.ReadInt32();
        var fragmentShader = reader.ReadBytes(fragmentShaderLength);

        return new MaterialAsset
        {
            VertexShader = vertexShader,
            FragmentShader = fragmentShader
        };
    }
}