using System.Diagnostics;
using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class ArrayBufferManager
{
    private uint BufferKind => GL_ARRAY_BUFFER;
    private ArrayBufferHandle m_BoundResource;

    private readonly Dictionary<ArrayBufferHandle, BufferMetadata> m_MetadataByHandleLookup = new();
    
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
            Debug.Assert(buffer != ArrayBufferHandle.Null, "No resource bound!");
            fixed (void* dataPtr = &data[0])
                glBufferStorage(BufferKind, SizeOf<T>(data.Length), dataPtr, (uint)accessFlag);
            AssertNoGlError();
            metadata.IsFixedSize = true;
            metadata.IsAllocated = true;
        }
    }

    public void Destroy(ArrayBufferHandle bufferHandle)
    {
        unsafe
        {
            if (m_BoundResource == bufferHandle)
            {
                glBindBuffer(BufferKind, ArrayBufferHandle.Null);
                AssertNoGlError();
                m_BoundResource = ArrayBufferHandle.Null;
            }

            uint id = bufferHandle;
            glDeleteBuffers(1, &id);
            AssertNoGlError();
            m_MetadataByHandleLookup.Remove(bufferHandle);
        }
    }

    public bool IsAllocated(ArrayBufferHandle handle)
    {
        var metadata = GetMetadata(handle);
        return metadata.IsAllocated;
    }
    
    public bool IsFixedSize(ArrayBufferHandle handle)
    {
        var metadata = GetMetadata(handle);
        return metadata.IsFixedSize;
    }

    private BufferMetadata GetMetadata(ArrayBufferHandle handle)
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