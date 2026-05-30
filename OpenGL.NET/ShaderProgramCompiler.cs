using System.Text;
using static OpenGLSandbox.OpenGlUtils;
using static GL46;

namespace OpenGL.NET;

public sealed class ShaderProgramCompiler
{
    private string? _vertexShaderSource;
    private string? _fragmentShaderSource;
    private string? _geometryShaderSource;

    public ShaderProgramCompiler WithVertexShader(string filePath)
    {
        _vertexShaderSource = File.ReadAllText(filePath);
        return this;
    }

    public ShaderProgramCompiler WithGeometryShader(string filePath)
    {
        _geometryShaderSource = File.ReadAllText(filePath);
        return this;
    }

    public ShaderProgramCompiler WithFragmentShader(string filePath)
    {
        _fragmentShaderSource = File.ReadAllText(filePath);
        return this;
    }

    public ShaderProgramCompiler WithVertexShaderSource(string source)
    {
        _vertexShaderSource = source;
        return this;
    }

    public ShaderProgramCompiler WithGeometryShaderSource(string source)
    {
        _geometryShaderSource = source;
        return this;
    }

    public ShaderProgramCompiler WithFragmentShaderSource(string source)
    {
        _fragmentShaderSource = source;
        return this;
    }

    public unsafe ShaderProgramInfo Compile()
    {
        var shaderProgramId = glCreateProgram(); AssertNoGlError();

        var vertexShader = CompileAndAttachShader(shaderProgramId, GL_VERTEX_SHADER, _vertexShaderSource);
        var fragmentShader = CompileAndAttachShader(shaderProgramId, GL_FRAGMENT_SHADER, _fragmentShaderSource);
        var geometryShader = CompileAndAttachShader(shaderProgramId, GL_GEOMETRY_SHADER, _geometryShaderSource);

        glLinkProgram(shaderProgramId); AssertNoGlError();

        int status;
        glGetProgramiv(shaderProgramId, GL_LINK_STATUS, &status); AssertNoGlError();

        if (status == GL_FALSE)
        {
            Span<byte> buffer = stackalloc byte[256];
            int length;
            fixed (byte* ptr = &buffer[0])
                glGetProgramInfoLog(shaderProgramId, 256, &length, ptr); AssertNoGlError();
            var log = Encoding.ASCII.GetString(buffer);
            Console.WriteLine($"Linking Failed: {log}");
        }

        glDeleteShader(vertexShader); AssertNoGlError();
        glDeleteShader(fragmentShader); AssertNoGlError();
        glDeleteShader(geometryShader); AssertNoGlError();

        return new ShaderProgramInfo
        {
            Id = shaderProgramId,
        };
    }

    private uint CompileAndAttachShader(uint shaderProgramId, uint type, string? shaderSource)
    {
        uint shaderId = 0;
        if (!string.IsNullOrEmpty(shaderSource))
        {
            shaderId = CreateAndCompileShaderFromSource(type, shaderSource);
            glAttachShader(shaderProgramId, shaderId); AssertNoGlError();
        }
        return shaderId;
    }
}