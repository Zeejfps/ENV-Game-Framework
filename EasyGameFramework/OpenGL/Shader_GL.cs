using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

public class Shader_GL : IGpuShader
{
    private readonly Dictionary<string, IBuffer> m_NameToBufferMap = new();
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();

    private readonly ITextureManager m_TextureManager;
    private readonly Dictionary<string, int> m_TextureToSlotMap = new();
    private int m_ActiveTextureId;

    private Shader_GL(uint id, ITextureManager textureManager)
    {
        Id = id;
        m_TextureManager = textureManager;
        IsLoaded = true;
    }

    public bool IsLoaded { get; }
    public uint Id { get; }

    public void SetFloat(string propertyName, float value)
    {
        var location = GetUniformLocation(propertyName);
        glUniform1f(location, value);
    }

    public void SetVector2(string propertyName, Vector2 value)
    {
        var location = GetUniformLocation(propertyName);
        glUniform2f(location, value.X, value.Y);
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

    public void SetTexture2d(string propertyName, IHandle<IGpuTexture> textureHandle)
    {
        var location = GetUniformLocation(propertyName);
        var textureSlot = GetTextureSlot(propertyName);
        glUniform1i(location, textureSlot);
        glActiveTexture(GL_TEXTURE0 + textureSlot);
        m_TextureManager.Bind(textureHandle);
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

    public IBuffer GetBuffer(string name)
    {
        if (m_NameToBufferMap.TryGetValue(name, out var buffer))
            return buffer;

        var index = glGetProgramResourceIndex(Id, GL_SHADER_STORAGE_BLOCK, name);
        glAssertNoError();

        buffer = new ShaderStorageBuffer_GL(index);
        m_NameToBufferMap[name] = buffer;
        return buffer;
    }

    public void Dispose()
    {
    }

    private int GetUniformLocation(string uniformName)
    {
        var propertyToIdMap = m_PropertyToIdMap;
        if (!propertyToIdMap.TryGetValue(uniformName, out var location))
        {
            location = glGetUniformLocation(Id, uniformName);
            propertyToIdMap[uniformName] = location;
        }

        return location;
    }

    private int GetTextureSlot(string texture)
    {
        var textureToSlotMap = m_TextureToSlotMap;
        if (textureToSlotMap.TryGetValue(texture, out var slot))
            return slot;

        slot = m_ActiveTextureId;
        textureToSlotMap[texture] = slot;
        m_ActiveTextureId++;
        return slot;
    }


    public static Shader_GL LoadFromSource(string vertexShaderSource, string fragmentShaderSource,
        ITextureManager textureManager)
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

        return new Shader_GL(program, textureManager);
    }

    private static void CompileShader(uint shader, string source)
    {
        glShaderSource(shader, source);
        glCompileShader(shader);

        var error = glGetShaderInfoLog(shader);
        if (!string.IsNullOrEmpty(error))
            throw new Exception($"Error compiling shader: {error}");
    }
}