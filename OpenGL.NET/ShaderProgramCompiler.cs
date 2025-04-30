using System.Text;
using static OpenGLSandbox.OpenGlUtils;
using static GL46;

namespace OpenGL.NET;

public sealed class ShaderProgramCompiler
{
    private string? _vertexShaderFilePath;
    private string? _fragmentShaderFilePath;
    private readonly HashSet<string> _uniforms = new();

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

    public ShaderProgramCompiler WithUniform(string uniformName)
    {
        _uniforms.Add(uniformName);
        return this;
    }

    public unsafe ShaderProgramInfo Compile()
    {
        var shaderProgram = glCreateProgram(); AssertNoGlError();

        uint vertexShader = 0;
        var vertexShaderFilePath = _vertexShaderFilePath;
        if (!string.IsNullOrEmpty(vertexShaderFilePath))
        {
            vertexShader = CreateAndCompileShaderFromSourceFile(GL_VERTEX_SHADER, vertexShaderFilePath);
            glAttachShader(shaderProgram, vertexShader); AssertNoGlError();
        }

        uint fragmentShader = 0;
        var fragmentShaderFilepath = _fragmentShaderFilePath;
        if (!string.IsNullOrEmpty(fragmentShaderFilepath))
        {
            fragmentShader = CreateAndCompileShaderFromSourceFile(GL_FRAGMENT_SHADER, fragmentShaderFilepath);
            glAttachShader(shaderProgram, fragmentShader); AssertNoGlError();
        }

        glLinkProgram(shaderProgram); AssertNoGlError();

        int status;
        glGetProgramiv(shaderProgram, GL_LINK_STATUS, &status); AssertNoGlError();

        if (status == GL_FALSE)
        {
            Span<byte> buffer = stackalloc byte[256];
            int length;
            fixed (byte* ptr = &buffer[0])
                glGetProgramInfoLog(shaderProgram, 256, &length, ptr); AssertNoGlError();
            var log = Encoding.ASCII.GetString(buffer);
            Console.WriteLine($"Linking Failed: {log}");
        }

        glDeleteShader(vertexShader); AssertNoGlError();
        glDeleteShader(fragmentShader); AssertNoGlError();

        return new ShaderProgramInfo
        {
            Id = 0,
        };
    }
}