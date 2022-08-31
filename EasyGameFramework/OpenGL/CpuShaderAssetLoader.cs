using System.Text;
using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.OpenGL;

public class CpuShaderAssetLoader : AssetLoader<ICpuShader>
{
    // protected override ICpuShader Load(Stream stream)
    // {
    //     using var reader = new BinaryReader(stream);
    //     var shader = CpuShader.Deserialize(reader);
    //     return shader;
    // }

    protected override string FileExtension => ".shader";

    protected override ICpuShader Load(Stream stream)
    {
        using var reader = new StreamReader(stream);

        string? vertexSource = null;
        string? fragmentSource = null;
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            line = line.Trim();
            switch (line)
            {
                case "#BEGIN vertex_shader":
                    vertexSource = ReadSource(reader);
                    break;
                case "#BEGIN fragment_shader":
                    fragmentSource = ReadSource(reader);
                    break;
            }
        }

        if (string.IsNullOrEmpty(vertexSource))
            throw new Exception("Missing vertex shader source code!");

        if (string.IsNullOrEmpty(fragmentSource))
            throw new Exception("Missing fragment shader source code!");

        return new CpuShader
        {
            VertexShader = vertexSource,
            FragmentShader = fragmentSource
        };
    }

    private string ReadSource(StreamReader reader)
    {
        var sb = new StringBuilder();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line == "#END")
                return sb.ToString();

            sb.AppendLine(line);
        }

        throw new Exception("Missing #end tag");
    }
}