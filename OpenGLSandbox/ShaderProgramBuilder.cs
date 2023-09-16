using static OpenGL.Gl;
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

        uint vertexShader = 0;
        if (m_VertexShaderFilePath != null)
        {
            vertexShader = CreateAndCompileShaderFromSourceFile(GL_VERTEX_SHADER, m_VertexShaderFilePath);
            glAttachShader(shaderProgram, vertexShader);
        }

        uint fragmentShader = 0;
        if (m_FragmentShaderFilePath != null)
        {
            fragmentShader = CreateAndCompileShaderFromSourceFile(GL_FRAGMENT_SHADER, m_FragmentShaderFilePath);
            glAttachShader(shaderProgram, fragmentShader);
        }
        
        glLinkProgram(shaderProgram);
        
        int status;
        glGetProgramiv(shaderProgram, GL_LINK_STATUS, &status);
        if (status == GL_FALSE)
        {
            var log = glGetProgramInfoLog(shaderProgram);
            Console.WriteLine($"Linking Failed: {log}");
        }
        else
        {
            Console.WriteLine("Linking succeeded");
        }
        
        glDeleteShader(vertexShader);
        glDeleteShader(fragmentShader);

        return shaderProgram;
    }
}