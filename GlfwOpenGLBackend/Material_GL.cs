using System.Numerics;
using Framework;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend.AssetLoaders;

public class Material_GL : IMaterial
{
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();
    private readonly Dictionary<string, int> m_TextureToSlotMap = new();
    public bool IsLoaded { get; private set; }

    private uint m_ProgramId;
    private int m_ActiveTextureId = 0;

    public Material_GL(uint programId)
    {
        m_ProgramId = programId;
        IsLoaded = true;
    }

    public bool IsDepthTestEnabled { get; set; }
    public bool IsBackfaceCullingEnabled { get; set; }

    public void Use()
    {
        glUseProgram(m_ProgramId);
        
        if (IsDepthTestEnabled)
            glEnable(GL_DEPTH_TEST);
        else
            glDisable(GL_DEPTH_TEST);

        if (IsBackfaceCullingEnabled)
            glEnable(GL_CULL_FACE);
        else
            glDisable(GL_CULL_FACE);
    }
    
    public void Unload()
    {
        m_ProgramId = 0;
        IsLoaded = false;
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
        var textureSlot = GetTextureSlot(propertyName);
        glUniform1i(location, textureSlot);
        glActiveTexture(GL_TEXTURE0 + textureSlot);
        texture.Use();
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

    private int GetTextureSlot(string texture)
    {
        if (m_TextureToSlotMap.TryGetValue(texture, out var slot))
            return slot;

        slot = m_ActiveTextureId;
        m_TextureToSlotMap[texture] = slot;
        m_ActiveTextureId++;
        return slot;
    }

    public static Material_GL LoadFromSource(string vertexShaderSource, string fragmentShaderSource)
    {
        var vertexShader = glCreateShader(GL_VERTEX_SHADER);
        CompileShader(vertexShader, vertexShaderSource);

        var fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
        CompileShader(fragmentShader, fragmentShaderSource);
        
        var program = glCreateProgram();
        glAttachShader(program, vertexShader);
        glAttachShader(program, fragmentShader);

        glLinkProgram(program);

        var error = glGetProgramInfoLog(program);
        if (!string.IsNullOrEmpty(error))
            throw new Exception($"Error compiling program:\n{error}");
        
        glDeleteShader(vertexShader);
        glDeleteShader(fragmentShader);

        return new Material_GL(program);
    }
    
    private static void CompileShader(uint shader, string source)
    {
        glShaderSource(shader, source);
        glCompileShader(shader);
        
        var error = glGetShaderInfoLog(shader);
        if (!string.IsNullOrEmpty(error))
            throw new Exception($"Error compiling shader: {error}");
    }

    private static float[] ToFloatArray(Matrix4x4 matrix)
    {
        var data = new float[16];
        
        data[00] = matrix.M11;
        data[01] = matrix.M12;
        data[02] = matrix.M13;
        data[03] = matrix.M14;
        
        data[04] = matrix.M21;
        data[05] = matrix.M22;
        data[06] = matrix.M23;
        data[07] = matrix.M24;
        
        data[08] = matrix.M31;
        data[09] = matrix.M32;
        data[10] = matrix.M33;
        data[11] = matrix.M34;
        
        data[12] = matrix.M41;
        data[13] = matrix.M42;
        data[14] = matrix.M43;
        data[15] = matrix.M44;

        return data;
    }
}