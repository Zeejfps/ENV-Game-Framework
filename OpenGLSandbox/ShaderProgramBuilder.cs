using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed class ShaderProgramBuilder
{
    private string? m_VertexShaderFilePath;
    private string? m_FragmentShaderFilePath;
    
    public ShaderProgramBuilder WithVertexShader(string filePath)
    {
        m_VertexShaderFilePath = filePath;
        return this;
    }

    public ShaderProgramBuilder WithFragmentShader(string filePath)
    {
        m_FragmentShaderFilePath = filePath;
        return this;
    }

    public unsafe uint Build()
    {
        var shaderProgram = glCreateProgram();
        AssertNoGlError();

        uint vertexShader = 0;
        if (m_VertexShaderFilePath != null)
        {
            vertexShader = CreateAndCompileShaderFromSourceFile(GL_VERTEX_SHADER, m_VertexShaderFilePath);
            glAttachShader(shaderProgram, vertexShader);
            AssertNoGlError();
        }

        uint fragmentShader = 0;
        if (m_FragmentShaderFilePath != null)
        {
            fragmentShader = CreateAndCompileShaderFromSourceFile(GL_FRAGMENT_SHADER, m_FragmentShaderFilePath);
            glAttachShader(shaderProgram, fragmentShader);
            AssertNoGlError();
        }
        
        glLinkProgram(shaderProgram);
        AssertNoGlError();
        
        int status;
        glGetProgramiv(shaderProgram, GL_LINK_STATUS, &status);
        AssertNoGlError();
        
        if (status == GL_FALSE)
        {
            var buffer = stackalloc char[256];
            int length;
            glGetProgramInfoLog(shaderProgram, 256, &length, buffer);
            AssertNoGlError();
            var log = new Span<char>(buffer, length);
            Console.WriteLine($"Linking Failed: {log}");
        }
        
        glDeleteShader(vertexShader);
        AssertNoGlError();
        
        glDeleteShader(fragmentShader);
        AssertNoGlError();

        return shaderProgram;
    }
}