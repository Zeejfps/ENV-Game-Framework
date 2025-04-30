using System.Diagnostics;
using OpenGLSandbox;
using static GL46;
using static OpenGlWrapper.OpenGlUtilsTwo;

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
        buffer.Bind();
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
        AssertIsBound();
        m_BoundBuffer!.AllocFixedSizedAndUploadData(data, accessFlags);
    }

    public void AllocFixedSize<T>(int length, FixedSizedBufferAccessFlag accessFlags) where T : unmanaged
    {
        AssertIsBound();
        m_BoundBuffer!.AllocFixedSize<T>(length, accessFlags);
    }
    
    public IReadWriteBufferMemory<T> MapReadWrite<T>() where T : unmanaged
    {
        AssertIsBound();
        return m_BoundBuffer!.MapReadWrite<T>();
    }

    public IReadOnlyBufferMemory<T> MapRead<T>() where T : unmanaged
    {
        AssertIsBound();
        return m_BoundBuffer!.MapRead<T>();
    }
    
    public IWriteOnlyBufferMemory<T> MapWrite<T>() where T : unmanaged
    {
        AssertIsBound(); 
        return m_BoundBuffer!.MapWrite<T>();
    }

    public IReadWriteBufferMemoryRange<T> MapReadWriteRange<T>(int offset, int length, BufferMemoryRangeAccessFlag accessFlags) where T : unmanaged
    {
        return MapRangeUnsafe<T>(offset, length, GL_MAP_READ_BIT | GL_MAP_WRITE_BIT | (uint)accessFlags);
    }
    
    private BufferMemoryRange<T> MapRangeUnsafe<T>(int offset, int count, uint access) where T : unmanaged
    {
        unsafe
        {
            var ptr = glMapBufferRange(BufferKind, OpenGlUtils.SizeOf<T>(offset), OpenGlUtils.SizeOf<T>(count), access);
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