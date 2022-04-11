using System.Numerics;
using Framework;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend.AssetLoaders;

public class Material_GL : IMaterial
{
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();

    public string Shader { get; }
    public bool IsLoaded { get; private set; }

    private uint m_ProgramId;
    private int m_ActiveTextureId = 0;

    public Material_GL(uint programId)
    {
        m_ProgramId = programId;
        IsLoaded = true;
    }

    public void Use()
    {
        m_ActiveTextureId = 0;
        glUseProgram(m_ProgramId);
    }
    
    public void Unload()
    {
        m_ProgramId = 0;
        IsLoaded = false;
    }
    
    public void Apply(IShaderProgram shaderProgram)
    {
        throw new NotImplementedException();
    }
    
    public void SetFloat(string propertyName, float value)
    {
        var location = GetUniformLocation(propertyName);
        glUniform1f(location, value);
    }

    public void SetVector3(string propertyName, float x, float y, float z)
    {
        var location = GetUniformLocation(propertyName);
        glUniform3f(location, x, y, z);
    }

    public void SetVector3(string propertyName, Vector3 vector)
    {
        SetVector3(propertyName, vector.X, vector.Y, vector.Z);
    }

    public void SetTexture2d(string propertyName, ITexture texture)
    {
        var location = GetUniformLocation(propertyName);
        glUniform1i(location, m_ActiveTextureId);
        glActiveTexture(GL_TEXTURE0 + m_ActiveTextureId);
        texture.Use();
        m_ActiveTextureId++;
    }

    public void SetMatrix4x4(string propertyName, Matrix4x4 matrix)
    {
        var location = GetUniformLocation(propertyName);
        unsafe
        {
            var p = &matrix.M11;
            glUniformMatrix4fv(location, 1, false, p);
        }
    }
    
    private int GetUniformLocation(string uniformName)
    {
        if (!m_PropertyToIdMap.TryGetValue(uniformName, out var location))
        {
            location = glGetUniformLocation(m_ProgramId, uniformName);
            m_PropertyToIdMap[uniformName] = location;
        }

        return location;
    }
}