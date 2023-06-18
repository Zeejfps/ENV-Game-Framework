using System.Numerics;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

public class Shader_GL : IGpuShader
{
    private readonly Dictionary<string, IBufferHandle> m_NameToBufferMap = new();
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();
    private readonly Dictionary<string, uint> m_BlockNameToIdTable = new();

    private readonly ITextureController m_TextureManager;
    private readonly Dictionary<string, int> m_TextureToSlotMap = new();
    private int m_ActiveTextureId;

    private Shader_GL(uint id, ITextureController textureManager)
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
        glAssertNoError();
    }

    public void SetVector2(string propertyName, Vector2 value)
    {
        var location = GetUniformLocation(propertyName);
        glUniform2f(location, value.X, value.Y);
        glAssertNoError();
    }
    
    public void SetVector3(string propertyName, float x, float y, float z)
    {
        var location = GetUniformLocation(propertyName);
        glUniform3f(location, x, y, z);
        glAssertNoError();
    }

    public void SetVector3(string propertyName, Vector3 vector)
    {
        SetVector3(propertyName, vector.X, vector.Y, vector.Z);
    }

    public void SetTexture2d(string propertyName, IGpuTextureHandle textureHandle)
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
            glAssertNoError();
        }
    }

    private int GetUniformLocation(string uniformName)
    {
        var propertyToIdMap = m_PropertyToIdMap;
        if (!propertyToIdMap.TryGetValue(uniformName, out var location))
        {
            location = glGetUniformLocation(Id, uniformName);
            glAssertNoError();
            propertyToIdMap[uniformName] = location;
        }

        return location;
    }

    private uint GetUniformBlockIndex(string uniformBlockName)
    {
        var blockNameToIdTable = m_BlockNameToIdTable;
        if (!blockNameToIdTable.TryGetValue(uniformBlockName, out var id))
        {
            id = glGetUniformBlockIndex(Id, uniformBlockName);
            glAssertNoError();
            blockNameToIdTable[uniformBlockName] = id;
        }

        return id;
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
        ITextureController textureManager)
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
        glAssertNoError();

        glCompileShader(shader);
        glAssertNoError();
        
        var error = glGetShaderInfoLog(shader);
        if (!string.IsNullOrEmpty(error))
            throw new Exception(error);
    }

    public void SetVector3Array(string uniformName, ReadOnlySpan<Vector3> array)
    {
        var location = GetUniformLocation(uniformName);
        unsafe
        {
            fixed (float* ptr = &array[0].X)
            {
                glUniform3fv(location, array.Length, ptr);
                glAssertNoError();
            }
        }
    }
    
    public void SetMatrix4x4Array(string uniformName, ReadOnlySpan<Matrix4x4> array)
    {
        var location = GetUniformLocation(uniformName);
        unsafe
        {
            fixed (float* ptr = &array[0].M11)
            {
                glUniformMatrix4fv(location, array.Length, false, ptr);
                glAssertNoError();
            }
        }
    }

    public void SetVector2Array(string uniformName, ReadOnlySpan<Vector2> array)
    {
        var location = GetUniformLocation(uniformName);
        unsafe
        {
            fixed (float* ptr = &array[0].X)
            {
                glUniform2fv(location, array.Length, ptr);
                glAssertNoError();
            }
        }
    }

    public void AttachBuffer(string bufferName, uint bindingPoint, uint bufferId)
    {
        glBindBufferBase(GL_UNIFORM_BUFFER, bindingPoint, bufferId);
        
        var index = GetUniformBlockIndex(bufferName);
        glUniformBlockBinding(Id, index, bindingPoint);
    }
    
    public void AttachShaderStorageBuffer(string bufferName, uint bindingPoint, uint bufferId)
    {
        glBindBufferBase(GL_SHADER_STORAGE_BUFFER, bindingPoint, bufferId);
    }
}