using System.Text;
using static OpenGLSandbox.OpenGlUtils;
using static GL46;

namespace OpenGL.NET;

public sealed class ShaderProgramCompiler
{
    private string? _vertexShaderFilePath;
    private string? _fragmentShaderFilePath;
    private string? _geometryShaderFilePath;

    public ShaderProgramCompiler WithVertexShader(string filePath)
    {
        _vertexShaderFilePath = filePath;
        return this;
    }
    
    public ShaderProgramCompiler WithGeometryShader(string filePath)
    {
        _geometryShaderFilePath = filePath;
        return this;
    }

    public ShaderProgramCompiler WithFragmentShader(string filePath)
    {
        _fragmentShaderFilePath = filePath;
        return this;
    }

    public unsafe ShaderProgramInfo Compile()
    {
        var shaderProgramId = glCreateProgram(); AssertNoGlError();

        var vertexShader = CompileAndAttachShader(shaderProgramId, GL_VERTEX_SHADER, _vertexShaderFilePath);
        var fragmentShader = CompileAndAttachShader(shaderProgramId, GL_FRAGMENT_SHADER, _fragmentShaderFilePath);
        var geometryShader = CompileAndAttachShader(shaderProgramId, GL_GEOMETRY_SHADER, _geometryShaderFilePath);

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

    private uint CompileAndAttachShader(uint shaderProgramId, uint type, string? shaderFilePath)
    {
        uint shaderId = 0;
        if (!string.IsNullOrEmpty(shaderFilePath))
        {
            shaderId = CreateAndCompileShaderFromSourceFile(type, shaderFilePath);
            glAttachShader(shaderProgramId, shaderId); AssertNoGlError();
        }
        return shaderId;
    }
}