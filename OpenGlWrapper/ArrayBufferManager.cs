using System.Diagnostics;
using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class ArrayBufferManager
{
    private uint BufferKind = GL_ARRAY_BUFFER;
    private ArrayBufferHandle m_BoundResource;
    
    public void Bind(ArrayBufferHandle handle)
    {
        glBindBuffer(BufferKind, handle);
        AssertNoGlError();
        m_BoundResource = handle;
    }
    
    public ArrayBufferHandle CreateAndBind()
    {
        unsafe
        {
            uint id;
            glGenBuffers(1, &id);
            AssertNoGlError();
            
            var handle = new ArrayBufferHandle(id);
            Bind(handle);
            
            return handle;
        }
    }

    public void AllocImmutableAndUpload()
    {
        Debug.Assert(m_BoundResource != ArrayBufferHandle.Null, "No resource bound!");
        //glBufferStorage(BufferKind, );
    }

    public void Destroy(ArrayBufferHandle bufferHandle)
    {
        unsafe
        {
            if (m_BoundResource == bufferHandle)
            {
                glBindBuffer(BufferKind, ArrayBufferHandle.Null);
                AssertNoGlError();
            }

            uint id = bufferHandle;
            glDeleteBuffers(1, &id);
            AssertNoGlError();
        }
    }
}

public readonly struct ArrayBufferHandle : IEquatable<ArrayBufferHandle>
{
    public static ArrayBufferHandle Null => new(0);
    
    public uint Id { get; }

    public ArrayBufferHandle(uint id)
    {
        Id = id;
    }

    public bool Equals(ArrayBufferHandle other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArrayBufferHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(ArrayBufferHandle left, ArrayBufferHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ArrayBufferHandle left, ArrayBufferHandle right)
    {
        return !left.Equals(right);
    }

    public static implicit operator uint(ArrayBufferHandle handle)
    {
        return handle.Id;
    }
}