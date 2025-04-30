using System.Text;
using static OpenGLSandbox.OpenGlUtils;
using static GL46;

namespace OpenGL.NET;

public sealed class ShaderProgramCompiler
{
    private string? _vertexShaderFilePath;
    private string? _fragmentShaderFilePath;

    public ShaderProgramCompiler WithVertexShader(string filePath)
    {
        _vertexShaderFilePath = filePath;
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

        uint vertexShader = 0;
        var vertexShaderFilePath = _vertexShaderFilePath;
        if (!string.IsNullOrEmpty(vertexShaderFilePath))
        {
            vertexShader = CreateAndCompileShaderFromSourceFile(GL_VERTEX_SHADER, vertexShaderFilePath);
            glAttachShader(shaderProgramId, vertexShader); AssertNoGlError();
        }

        uint fragmentShader = 0;
        var fragmentShaderFilepath = _fragmentShaderFilePath;
        if (!string.IsNullOrEmpty(fragmentShaderFilepath))
        {
            fragmentShader = CreateAndCompileShaderFromSourceFile(GL_FRAGMENT_SHADER, fragmentShaderFilepath);
            glAttachShader(shaderProgramId, fragmentShader); AssertNoGlError();
        }

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

        return new ShaderProgramInfo
        {
            Id = shaderProgramId,
        };
    }
}