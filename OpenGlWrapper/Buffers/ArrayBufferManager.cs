using System.Diagnostics;
using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper.Buffers;

public sealed class ArrayBufferManager
{
    public ArrayBufferManager()
    {
    }

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

    public void AllocFixedSizedAndUploadData<T>(ReadOnlySpan<T> data, FixedSizedBufferAccessFlag accessFlags) where T : unmanaged
    {
        unsafe
        {
            fixed (void* dataPtr = &data[0])
                AllocFixeSizeUnsafe<T>(data.Length, dataPtr, accessFlags);
        }
    }

    public void AllocFixedSize<T>(int length, FixedSizedBufferAccessFlag accessFlags) where T : unmanaged
    {
        unsafe
        {
            AllocFixeSizeUnsafe<T>(length, null, accessFlags);
        }
    }

    private unsafe void AllocFixeSizeUnsafe<T>(int length, void* dataPtr, FixedSizedBufferAccessFlag accessFlags) where T : unmanaged
    {
        AssertIsBound();
        var buffer = m_BoundResource;
        var metadata = GetMetadata(buffer);
        if (metadata.IsAllocated && metadata.IsFixedSize)
            throw new InvalidOperationException($"Can't re-allocate an already allocated FIXED-SIZED buffer, Id: {buffer.Id}");

        var size = SizeOf<T>(length);
        glBufferStorage(BufferKind, size, dataPtr, (uint)accessFlags);
        AssertNoGlError();
        metadata.IsFixedSize = true;
        metadata.IsAllocated = true;
        metadata.AccessFlags = accessFlags;
        metadata.Length = length;
    }
    
    public IReadWriteBufferMemory<T> MapReadWrite<T>() where T : unmanaged
    {
        AssertIsBound();
        var metadata = GetMetadata(m_BoundResource);
        if ((metadata.AccessFlags & FixedSizedBufferAccessFlag.Read) == 0 ||
            (metadata.AccessFlags & FixedSizedBufferAccessFlag.Write) == 0)
            throw new InvalidOperationException($"Can't map buffer for read-write access, missing access flags. Required Read and Write flag, found: {metadata.AccessFlags}");

        return Map<T>(metadata.Length);
    }

    public IReadOnlyBufferMemory<T> MapRead<T>() where T : unmanaged
    {
        AssertIsBound();
        var metadata = GetMetadata(m_BoundResource);
        if ((metadata.AccessFlags & FixedSizedBufferAccessFlag.Read) == 0)
            throw new InvalidOperationException($"Can't map buffer for read access, missing access flags. Required Read flag, found: {metadata.AccessFlags}");
        return Map<T>(metadata.Length);
    }
    
    public IWriteOnlyBufferMemory<T> MapWrite<T>() where T : unmanaged
    {
        AssertIsBound();
        var metadata = GetMetadata(m_BoundResource);
        if ((metadata.AccessFlags & FixedSizedBufferAccessFlag.Write) == 0)
            throw new InvalidOperationException($"Can't map buffer for write access, missing access flags. Required Write flag, found: {metadata.AccessFlags}");
        return Map<T>(metadata.Length);
    }

    private BufferMemoryRange<T> Map<T>(int length) where T : unmanaged
    {
        unsafe
        {
            var ptr = glMapBuffer(BufferKind, GL_READ_WRITE);
            AssertNoGlError();
            return new BufferMemoryRange<T>(BufferKind, 0, ptr, length);
        }
    }
    
    public IBufferMemory<T> MapRange<T>(int offset, int length, MappedBufferAccessFlag accessFlag) where T : unmanaged
    {
        var sizeOf = SizeOf<T>();
        //glMapBufferRange(BufferKind, sizeOf * startIndex, sizeOf * length,);
        return null;
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

    [Conditional("DEBUG")]
    private void AssertIsBound()
    {
        Debug.Assert(m_BoundResource != ArrayBufferId.Null, "No resource bound");
    }
    
}

class BufferMetadata
{
    public bool IsAllocated { get; set; }
    public bool IsFixedSize { get; set; }
    public FixedSizedBufferAccessFlag AccessFlags { get; set; } = FixedSizedBufferAccessFlag.None;
    public int Length { get; set; }
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