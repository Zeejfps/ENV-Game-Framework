using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class VertexArrayObjectManager
{
    private readonly ArrayBufferManager m_ArrayBufferManager;
    private VertexArrayObjectHandle m_BoundResource;

    public VertexArrayObjectManager(ArrayBufferManager arrayBufferManager)
    {
        m_ArrayBufferManager = arrayBufferManager;
    }
    
    public void Bind(VertexArrayObjectHandle handle)
    {
        glBindVertexArray(handle);
        AssertNoGlError();
        m_BoundResource = handle;
    }
    
    public VertexArrayObjectHandle CreateAndBind()
    {
        unsafe
        {
            uint id;
            glGenVertexArrays(1, &id);
            AssertNoGlError();

            var handle = new VertexArrayObjectHandle(id);
            Bind(handle);

            return handle;
        }
    }

    public void Destroy(VertexArrayObjectHandle vao)
    {
        if (m_BoundResource == vao)
            Bind(VertexArrayObjectHandle.Null);
        
        unsafe
        {
            var id = vao.Id;
            glDeleteVertexArrays(1, &id);
            AssertNoGlError();
        }
    }

    public VertexArrayObjectManager EnableAndBindAttrib(int attribIndex, ArrayBufferHandle vbo, int size, GlType type, bool normalized, int stride, int offset)
    {
        if (m_BoundResource == VertexArrayObjectHandle.Null)
            throw new InvalidOperationException("No resource bound");
        
        unsafe
        {
            m_ArrayBufferManager.Bind(vbo);
            glVertexAttribPointer((uint)attribIndex, size, (uint)type, normalized, stride, Offset(offset));
            return this;
        }
    }
}

public readonly struct VertexArrayObjectHandle : IEquatable<VertexArrayObjectHandle>
{
    public static VertexArrayObjectHandle Null => new(0);
    
    internal uint Id { get; }

    public VertexArrayObjectHandle(uint id)
    {
        Id = id;
    }

    public bool Equals(VertexArrayObjectHandle other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is VertexArrayObjectHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(VertexArrayObjectHandle left, VertexArrayObjectHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VertexArrayObjectHandle left, VertexArrayObjectHandle right)
    {
        return !left.Equals(right);
    }
    
    public static implicit operator uint(VertexArrayObjectHandle handle)
    {
        return handle.Id;
    }
}