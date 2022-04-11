using System.Numerics;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class ShaderProgram_GL : IShaderProgram
{
    private uint m_Id;
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();
    private readonly Dictionary<string, ITexture> m_PropertyToTextureMap = new();

    private int m_ActiveTextureId = 0;
    
    public ShaderProgram_GL(string shader)
    {
        m_Id = CreateProgram(shader);
    }
    
    public void Use()
    {
        m_ActiveTextureId = 0;
        glUseProgram(m_Id);

        // var samplerId = GL_TEXTURE0;
        // foreach (var kvp in m_PropertyToTextureMap)
        // {
        //     var uniform = kvp.Key;
        //     var texture = kvp.Value;
        //     var location = GetUniformLocation(uniform);
        //     glUniform1i(location, samplerId);
        //     glActiveTexture(samplerId);
        //     texture.Use();
        //     samplerId++;
        // }
    }
    
    public void SetVector3f(string propertyName, float x, float y, float z)
    {
        var location = GetUniformLocation(propertyName);
        glUniform3f(location, x, y, z);
    }

    public void SetMatrix4x4f(string propertyName, Matrix4x4 matrix)
    {
        var location = GetUniformLocation(propertyName);
        unsafe
        {
            var p = &matrix.M11;
            glUniformMatrix4fv(location, 1, false, p);
        }
    }
    public void SetFloat(string propertyName, float x)
    {
        var location = GetUniformLocation(propertyName);
        glUniform1f(location, x);
    }

    public void SetTexture2d(string propertyName, ITexture texture)
    {
        var location = GetUniformLocation(propertyName);
        glUniform1i(location, m_ActiveTextureId);
        glActiveTexture(GL_TEXTURE0 + m_ActiveTextureId);
        texture.Use();
        m_ActiveTextureId++;
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