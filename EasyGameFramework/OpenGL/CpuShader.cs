using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.AssetManagement;

public class CpuShader : ICpuShader
{
    public string VertexShader { get; set; } = string.Empty;
    public string FragmentShader { get; set; } = string.Empty;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(VertexShader);
        writer.Write(FragmentShader);
    }

    public static CpuShader Deserialize(BinaryReader reader)
    {
        var vertexShader = reader.ReadString();
        var fragmentShader = reader.ReadString();

        return new CpuShader
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