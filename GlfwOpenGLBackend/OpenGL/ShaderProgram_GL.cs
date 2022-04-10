using Framework;
using GLFW;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class ShaderProgram_GL : IShaderProgram
{
    private uint m_Id;
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();

    public ShaderProgram_GL(string shader)
    {
        m_Id = CreateProgram(shader);
    }
    
    public void Use()
    {
        glUseProgram(m_Id);
    }
    
    public void SetVector3f(string propertyName, float x, float y, float z)
    {
        var location = GetUniformLocation(propertyName);
        glUniform3f(location, x, y, z);
    }

    public void SetMatrix4x4f(string propertyName, float[] matrix)
    {
        var location = GetUniformLocation(propertyName);
        glUniformMatrix4fv(location, 1, false, matrix);
    }
    public void SetFloat(string propertyName, float x)
    {
        var location = GetUniformLocation(propertyName);
        glUniform1f(location, x);
    }
    private int GetUniformLocation(string uniformName)
    {
        if (!m_PropertyToIdMap.TryGetValue(uniformName, out var location))
        {
            location = glGetUniformLocation(m_Id, uniformName);
            m_PropertyToIdMap.Add(uniformName, location);
        }

        return location;
    }

    private uint CreateProgram(string pathToShader)
    {
        var vertex = CreateShader(GL_VERTEX_SHADER, File.ReadAllText($"{pathToShader}.vert"));
        var fragment = CreateShader(GL_FRAGMENT_SHADER, File.ReadAllText($"{pathToShader}.frag"));
            
        var program = glCreateProgram();
        glAttachShader(program, vertex);
        glAttachShader(program, fragment);

        glLinkProgram(program);

        glDeleteShader(vertex);
        glDeleteShader(fragment);

        return program;
    }
    
    private uint CreateShader(int type, string source)
    {
        var shader = glCreateShader(type);
        glShaderSource(shader, source);
        glCompileShader(shader);
        
        var error = glGetShaderInfoLog(shader);
        if (!string.IsNullOrEmpty(error))
            Console.WriteLine($"Error compiling {type} shader: {error}");
        
        return shader;
    }
}