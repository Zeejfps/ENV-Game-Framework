using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public enum ArrayBufferUsageHint : int
{
    DynamicDraw = GL_DYNAMIC_DRAW,
    StaticDraw = GL_STATIC_DRAW,
}

public sealed class NotAllocatedException : Exception
{
    public NotAllocatedException() : base("Buffer is not allocated")
    {
        
    }
}

public sealed class ArrayBuffer<T> where T : unmanaged
{
    private const int BindTarget = GL_ARRAY_BUFFER;

    private ArrayBufferUsageHint m_UsageHint;
    private IntPtr m_Size;
    private bool m_IsAllocated;
    private uint m_Id;

    public void Bind()
    {
        glBindBuffer(BindTarget, m_Id);
    }

    public void Alloc(int size, ArrayBufferUsageHint usageHint)
    {
        if (m_IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            AllocUnsafe(size, (void*)0, usageHint);
        }
    }

    public void AllocAndWrite(T data, ArrayBufferUsageHint usageHint)
    {
        if (m_IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            AllocUnsafe(1, &data, usageHint);
        }
    }

    public void AllocAndWrite(ReadOnlySpan<T> data, ArrayBufferUsageHint usageHint)
    {
        if (m_IsAllocated)
            throw new NotAllocatedException();

        unsafe
        {
            fixed (void* dataPtr = &data[0])
            {
                AllocUnsafe(data.Length, dataPtr, usageHint);
            }
        }
    }

    private unsafe void AllocUnsafe(int size, void* data, ArrayBufferUsageHint usageHint)
    {
        var sizePtr = SizeOf<T>(size);
        m_Id = glGenBuffer();
        glBindBuffer(BindTarget, m_Id);
        AssertNoGlError();
        glBufferData(BindTarget, sizePtr, data, (int)usageHint);
        AssertNoGlError();
        m_Size = sizePtr;
        m_UsageHint = usageHint;
        m_IsAllocated = true;
    }

    public void ReAlloc()
    {
        unsafe
        {
            if (!m_IsAllocated)
                throw new NotAllocatedException();

            var sizePtr = m_Size;
            var usageHint = m_UsageHint;
            glBindBuffer(BindTarget, m_Id);
            AssertNoGlError();
            glBufferData(BindTarget, sizePtr, (void*)0, (int)usageHint);
            AssertNoGlError();
        }
    }

    public void Write(int offset, ReadOnlySpan<T> data)
    {
        if (!m_IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            glBindBuffer(BindTarget, m_Id);
            AssertNoGlError();
            fixed (void* ptr = &data[0])
            {
                glBufferSubData(BindTarget, new IntPtr(offset), SizeOf<T>(data.Length), ptr);
                AssertNoGlError();
            }
        }
    }
    
    // NOTE(Zee): This API may not be the best, but it works for now
    public void WriteMapped(int offset, int length, Action<GpuMemory<T>> writeFunc)
    {
        if (!m_IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            glBindBuffer(GL_ARRAY_BUFFER, m_Id);
            AssertNoGlError();
            var bufferPtr = glMapBufferRange(BindTarget, SizeOf<T>(offset), SizeOf<T>(length), GL_MAP_WRITE_BIT);
            AssertNoGlError();
            writeFunc.Invoke(new GpuMemory<T>(bufferPtr, length));
            glUnmapBuffer(BindTarget);
            AssertNoGlError();
        }
    }
    
    public void WriteMapped(Action<GpuMemory<T>> writeFunc)
    {
        if (!m_IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            glBindBuffer(GL_ARRAY_BUFFER, m_Id);
            AssertNoGlError();
            var bufferPtr = glMapBuffer(BindTarget, GL_WRITE_ONLY);
            AssertNoGlError();
            writeFunc.Invoke(new GpuMemory<T>(bufferPtr, m_Size.ToInt32()));
            glUnmapBuffer(BindTarget);
            AssertNoGlError();
        }
    }
    
    public void Free()
    {
        if (!m_IsAllocated)
            throw new NotAllocatedException();
        
        m_IsAllocated = false;
        unsafe
        {
            var id = m_Id;
            glDeleteBuffers(1, &id);
            AssertNoGlError();
        }
        m_Id = 0;
    }
}