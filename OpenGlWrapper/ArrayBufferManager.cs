using System.Diagnostics;
using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class ArrayBufferManager
{
    private uint BufferKind => GL_ARRAY_BUFFER;
    private ArrayBufferId m_BoundResource;

    private readonly Dictionary<ArrayBufferId, BufferMetadata> m_MetadataByHandleLookup = new();
    
    public void Bind(ArrayBufferId handle)
    {
        if (m_BoundResource == handle)
            return;
        
        glBindBuffer(BufferKind, handle);
        AssertNoGlError();
        m_BoundResource = handle;
    }
    
    public ArrayBufferId CreateAndBind()
    {
        unsafe
        {
            uint id;
            glGenBuffers(1, &id);
            AssertNoGlError();
            
            var handle = new ArrayBufferId(id);
            m_MetadataByHandleLookup.Add(handle, new BufferMetadata());
            Bind(handle);
            
            return handle;
        }
    }

    public void AllocFixedSizedAndUploadData<T>(ReadOnlySpan<T> data, FixedSizedBufferAccessFlag accessFlag) where T : unmanaged
    {
        var buffer = m_BoundResource;
        var metadata = GetMetadata(buffer);
        if (metadata.IsAllocated && metadata.IsFixedSize)
            throw new InvalidOperationException($"Can't re-allocate an already allocated FIXED-SIZED buffer, Id: {buffer.Id}");
        
        unsafe
        {
            Debug.Assert(buffer != ArrayBufferId.Null, "No resource bound!");
            fixed (void* dataPtr = &data[0])
                glBufferStorage(BufferKind, SizeOf<T>(data.Length), dataPtr, (uint)accessFlag);
            AssertNoGlError();
            metadata.IsFixedSize = true;
            metadata.IsAllocated = true;
        }
    }

    public void Destroy(ArrayBufferId bufferHandle)
    {
        unsafe
        {
            if (m_BoundResource == bufferHandle)
            {
                glBindBuffer(BufferKind, ArrayBufferId.Null);
                AssertNoGlError();
                m_BoundResource = ArrayBufferId.Null;
            }

            uint id = bufferHandle;
            glDeleteBuffers(1, &id);
            AssertNoGlError();
            m_MetadataByHandleLookup.Remove(bufferHandle);
        }
    }

    public bool IsAllocated(ArrayBufferId handle)
    {
        var metadata = GetMetadata(handle);
        return metadata.IsAllocated;
    }
    
    public bool IsFixedSize(ArrayBufferId handle)
    {
        var metadata = GetMetadata(handle);
        return metadata.IsFixedSize;
    }

    private BufferMetadata GetMetadata(ArrayBufferId handle)
    {
        if (!m_MetadataByHandleLookup.TryGetValue(handle, out var metadata))
            throw new ArgumentException($"Trying to access metadata for a non-existing buffer: {handle.Id}");
        return metadata;
    }
}

class BufferMetadata
{
    public bool IsAllocated { get; set; }
    public bool IsFixedSize { get; set; }
}

public readonly struct ArrayBufferId : IEquatable<ArrayBufferId>
{
    public static ArrayBufferId Null => new(0);
    
    internal uint Id { get; }

    public ArrayBufferId(uint id)
    {
        Id = id;
    }

    public bool Equals(ArrayBufferId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArrayBufferId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(ArrayBufferId left, ArrayBufferId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ArrayBufferId left, ArrayBufferId right)
    {
        return !left.Equals(right);
    }

    public static implicit operator uint(ArrayBufferId handle)
    {
        return handle.Id;
    }
}