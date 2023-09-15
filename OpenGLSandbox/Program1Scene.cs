using static OpenGL.Gl;

namespace OpenGLSandbox;

public sealed class Program1Scene
{
    const string VertexShaderSource = 
@"
#version 430

layout(location = 0) in vec4 position;

void main() {
    gl_Position = position;
}
";

    const string FragmentShaderSource =
@"
#version 430
out vec4 color;

void main() {
    color = vec4(0.6f, 0.1f, 1.0f, 1.0);
}
";
    
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_ShaderProgram;
    
    public unsafe void Load()
    {
        var verts = new[]
        {
            -0.90f, +0.85f, 
            +0.85f, -0.90f, 
            -0.90f, -0.90f,
            
            +0.90f, +0.90f, 
            +0.90f, -0.85f,
            -0.85f, +0.90f,
        };
        
        m_Vao = glGenVertexArray();
        glBindVertexArray(m_Vao);
        
        m_Vbo = glGenBuffer();
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        fixed (float* ptr = &verts[0]) 
            glBufferData(GL_ARRAY_BUFFER, verts.Length * sizeof(float), ptr, GL_STATIC_DRAW);

        uint positionAttribIndex = 0;
        glVertexAttribPointer(positionAttribIndex, 2, GL_FLOAT, false, 2 * sizeof(float), IntPtr.Zero);
        glEnableVertexAttribArray(positionAttribIndex);
        
        var vertexShader = CompileShader(GL_VERTEX_SHADER, VertexShaderSource);
        var fragmentShader = CompileShader(GL_FRAGMENT_SHADER, FragmentShaderSource);

        m_ShaderProgram = glCreateProgram();
        var shaderProgram = m_ShaderProgram;
        glAttachShader(shaderProgram, vertexShader);
        glAttachShader(shaderProgram, fragmentShader);
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
        
        glUseProgram(shaderProgram);
        glClearColor(1f, 0f, 1f, 1f);
    }

    private unsafe uint CompileShader(int type, string source)
    {
        var shader = glCreateShader(type);
        glShaderSource(shader, source);
        glCompileShader(shader);

        int status;
        glGetShaderiv(shader, GL_COMPILE_STATUS, &status);
        if (status == GL_FALSE)
        {
            var infoLog = glGetShaderInfoLog(shader);
            Console.WriteLine(infoLog);
            glDeleteShader(shader);
            return 0;
        }

        return shader;
    }

    public void Unload()
    {
        glDeleteVertexArray(m_Vao);
        glDeleteBuffer(m_Vbo);
        glDeleteProgram(m_ShaderProgram);
    }

    public void Render()
    {
        glDrawArrays(GL_TRIANGLES, 0, 6);
    }
}