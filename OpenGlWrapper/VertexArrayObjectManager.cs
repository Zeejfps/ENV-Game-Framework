using System.Diagnostics;
using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class VertexArrayObjectManager
{
    private readonly ArrayBufferManager m_ArrayBufferManager;
    private VertexArrayObjectId m_BoundResource;

    public VertexArrayObjectManager(ArrayBufferManager arrayBufferManager)
    {
        m_ArrayBufferManager = arrayBufferManager;
    }
    
    public void Bind(VertexArrayObjectId handle)
    {
        if (handle == m_BoundResource)
            return;
        glBindVertexArray(handle);
        AssertNoGlError();
        m_BoundResource = handle;
    }
    
    public VertexArrayObjectId CreateAndBind()
    {
        unsafe
        {
            uint id;
            glGenVertexArrays(1, &id);
            AssertNoGlError();

            var handle = new VertexArrayObjectId(id);
            Bind(handle);

            return handle;
        }
    }

    public void Destroy(VertexArrayObjectId vao)
    {
        if (m_BoundResource == vao)
            Bind(VertexArrayObjectId.Null);
        
        unsafe
        {
            var id = vao.Id;
            glDeleteVertexArrays(1, &id);
            AssertNoGlError();
        }
    }

    public VertexArrayObjectManager EnableAndBindAttrib(int attribIndex, ArrayBufferHandle vbo, int size, GlType type, bool normalized, int stride, int offset)
    {
        BindAttrib(attribIndex, vbo, size, type, normalized, stride, offset);
        EnableAttrib(attribIndex);
        return this;
    }

    public VertexArrayObjectManager EnableAttrib(int attribIndex)
    {
        AssertResourceIsBound();   
        glEnableVertexArrayAttrib(m_BoundResource, (uint)attribIndex);
        AssertNoGlError();
        return this;
    }

    public VertexArrayObjectManager BindAttrib(int attribIndex, ArrayBufferHandle vbo, int size, GlType type,
        bool normalized, int stride, int offset)
    {
        AssertResourceIsBound();   
        unsafe
        {
            m_ArrayBufferManager.Bind(vbo);
            glVertexAttribPointer((uint)attribIndex, size, (uint)type, normalized, stride, Offset(offset));
            AssertNoGlError();
            return this;
        }
    }

    [Conditional("DEBUG")]
    private void AssertResourceIsBound()
    {
        if (m_BoundResource == VertexArrayObjectId.Null)
            throw new InvalidOperationException("No resource bound");
    }
}

public readonly struct VertexArrayObjectId : IEquatable<VertexArrayObjectId>
{
    public static VertexArrayObjectId Null => new(0);
    
    internal uint Id { get; }

    public VertexArrayObjectId(uint id)
    {
        Id = id;
    }

    public bool Equals(VertexArrayObjectId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is VertexArrayObjectId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(VertexArrayObjectId left, VertexArrayObjectId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VertexArrayObjectId left, VertexArrayObjectId right)
    {
        return !left.Equals(right);
    }
    
    public static implicit operator uint(VertexArrayObjectId handle)
    {
        return handle.Id;
    }
}