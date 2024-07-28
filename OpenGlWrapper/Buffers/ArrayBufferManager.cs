using System.Diagnostics;
using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper.Buffers;

public sealed class ArrayBufferManager
{
    private uint BufferKind => GL_ARRAY_BUFFER;
    private ArrayBuffer? m_BoundBuffer;

    private readonly Dictionary<ArrayBufferHandle, ArrayBuffer> m_BufferByHandleLookup = new();
    
    public void Bind(ArrayBufferHandle handle)
    {
        if (m_BoundBuffer != null && m_BoundBuffer.Handle == handle)
            return;

        var buffer = GetBuffer(handle);
        glBindBuffer(BufferKind, handle);
        AssertNoGlError();
        m_BoundBuffer = buffer;
    }
    
    public ArrayBufferHandle CreateAndBind()
    {
        unsafe
        {
            uint id;
            glGenBuffers(1, &id);
            AssertNoGlError();
            
            var handle = new ArrayBufferHandle(id);
            m_BufferByHandleLookup.Add(handle, new ArrayBuffer
            {
                Handle = handle
            });
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
        var buffer = m_BoundBuffer!;
        if (buffer.IsAllocated && buffer.IsFixedSize)
            throw new InvalidOperationException($"Can't re-allocate an already allocated FIXED-SIZED buffer, Id: {buffer.Handle.Id}");

        var sizeInBytes = SizeOf<T>(length);
        glBufferStorage(BufferKind, sizeInBytes, dataPtr, (uint)accessFlags);
        AssertNoGlError();
        buffer.IsFixedSize = true;
        buffer.IsAllocated = true;
        buffer.AccessFlags = accessFlags;
        buffer.SizeInBytes = sizeInBytes.ToInt32();
    }
    
    public IReadWriteBufferMemory<T> MapReadWrite<T>() where T : unmanaged
    {
        AssertIsBound();
        var buffer = m_BoundBuffer!;
        if ((buffer.AccessFlags & FixedSizedBufferAccessFlag.Read) == 0 ||
            (buffer.AccessFlags & FixedSizedBufferAccessFlag.Write) == 0)
            throw new InvalidOperationException($"Can't map buffer for read-write access, missing access flags. Required Read and Write flag, found: {buffer.AccessFlags}");

        return MapUnsafe<T>(buffer.SizeInBytes, GL_READ_WRITE);
    }

    public IReadOnlyBufferMemory<T> MapRead<T>() where T : unmanaged
    {
        AssertIsBound();
        var boundBuffer = m_BoundBuffer!;
        if ((boundBuffer.AccessFlags & FixedSizedBufferAccessFlag.Read) == 0)
            throw new InvalidOperationException($"Can't map buffer for read access, missing access flags. Required Read flag, found: {boundBuffer.AccessFlags}");
        return MapUnsafe<T>(boundBuffer.SizeInBytes, GL_READ_ONLY);
    }
    
    public IWriteOnlyBufferMemory<T> MapWrite<T>() where T : unmanaged
    {
        AssertIsBound();
        var buffer = m_BoundBuffer!;
        if ((buffer.AccessFlags & FixedSizedBufferAccessFlag.Write) == 0)
            throw new InvalidOperationException($"Can't map buffer for write access, missing access flags. Required Write flag, found: {buffer.AccessFlags}");
        return MapUnsafe<T>(buffer.SizeInBytes, GL_WRITE_ONLY);
    }

    public IReadWriteBufferMemoryRange<T> MapReadWriteRange<T>(int offset, int length, BufferMemoryRangeAccessFlag accessFlags) where T : unmanaged
    {
        return MapRangeUnsafe<T>(offset, length, GL_MAP_READ_BIT | GL_MAP_WRITE_BIT | (uint)accessFlags);
    }

    private BufferMemoryRange<T> MapUnsafe<T>(int sizeInBytes, uint access) where T : unmanaged
    {
        unsafe
        {
            var ptr = glMapBuffer(BufferKind, access);
            AssertNoGlError();
            if (ptr == null)
                throw new Exception("Failed to map buffer, Unknown error");
            
            var sizeOfT = SizeOf<T>();
            var count = sizeInBytes / sizeOfT.ToInt32();
            return new BufferMemoryRange<T>(BufferKind, 0, ptr, count, (uint)BufferMemoryRangeAccessFlag.None);
        }
    }
    
    private BufferMemoryRange<T> MapRangeUnsafe<T>(int offset, int count, uint access) where T : unmanaged
    {
        unsafe
        {
            var ptr = glMapBufferRange(BufferKind, SizeOf<T>(offset), SizeOf<T>(count), access);
            AssertNoGlError();
            if (ptr == null)
                throw new Exception("Failed to map buffer range, Unknown error");
            
            return new BufferMemoryRange<T>(BufferKind, offset, ptr, count, access);
        }
    }

    public void Destroy(ArrayBufferHandle bufferHandle)
    {
        unsafe
        {
            Unbind(bufferHandle);
            uint id = bufferHandle;
            glDeleteBuffers(1, &id);
            AssertNoGlError();
            m_BufferByHandleLookup.Remove(bufferHandle);
        }
    }

    private void Unbind(ArrayBufferHandle bufferHandle)
    {
        if (m_BoundBuffer == null)
            return;
        
        if (m_BoundBuffer.Handle != bufferHandle)
            return;

        m_BoundBuffer = null;
        glBindBuffer(BufferKind, 0);
    }

    public bool IsAllocated(ArrayBufferHandle handle)
    {
        var metadata = GetBuffer(handle);
        return metadata.IsAllocated;
    }
    
    public bool IsFixedSize(ArrayBufferHandle handle)
    {
        var metadata = GetBuffer(handle);
        return metadata.IsFixedSize;
    }

    private ArrayBuffer GetBuffer(ArrayBufferHandle handle)
    {
        if (!m_BufferByHandleLookup.TryGetValue(handle, out var buffer))
            throw new ArgumentException($"Trying to access metadata for a non-existing buffer: {handle.Id}");
        return buffer;
    }

    [Conditional("DEBUG")]
    private void AssertIsBound()
    {
        Debug.Assert(m_BoundBuffer != null, "No resource bound");
    }
    
}

class ArrayBuffer
{
    public ArrayBufferHandle Handle { get; set; }
    public bool IsAllocated { get; set; }
    public bool IsFixedSize { get; set; }
    public FixedSizedBufferAccessFlag AccessFlags { get; set; } = FixedSizedBufferAccessFlag.None;
    public int SizeInBytes { get; set; }
}

public readonly struct ArrayBufferHandle : IEquatable<ArrayBufferHandle>
{
    public static ArrayBufferHandle Null => new(0);
    
    internal uint Id { get; }

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