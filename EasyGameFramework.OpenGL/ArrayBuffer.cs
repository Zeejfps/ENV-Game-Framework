using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public readonly struct GpuMemory<T>
{
    private readonly unsafe void* m_Ptr;
    private readonly int m_Length;
    
    public unsafe GpuMemory(void* ptr, int length)
    {
        m_Ptr = ptr;
        m_Length = length;
    }

    public Span<T> Span
    {
        get
        {
            unsafe
            {
                return new Span<T>(m_Ptr, m_Length);
            }
        }
    }
}

public enum ArrayBufferUsageHint : int
{
    DynamicDraw = GL_DYNAMIC_DRAW,
    StaticDraw = GL_STATIC_DRAW,
}

public sealed class ArrayBuffer<T> where T : unmanaged
{
    private const int BindTarget = GL_ARRAY_BUFFER;
    
    private bool m_IsAllocated;
    private uint m_Id;

    public void Bind()
    {
        glBindBuffer(BindTarget, m_Id);
    }

    public void Alloc(int size, ArrayBufferUsageHint usageHint)
    {
        if (m_IsAllocated)
            throw new Exception("Buffer is already allocated");
        
        unsafe
        {
            m_Id = glGenBuffer();
            glBindBuffer(BindTarget, m_Id);
            AssertNoGlError();
            glBufferData(BindTarget, SizeOf<T>(size), (void*)0, (int)usageHint);
            AssertNoGlError();
        }

        m_IsAllocated = true;
    }
    
    public void AllocAndWrite(ReadOnlySpan<T> data, ArrayBufferUsageHint usageHint)
    {
        if (m_IsAllocated)
            throw new Exception("Buffer is already allocated");
        
        unsafe
        {
            m_Id = glGenBuffer();
            glBindBuffer(BindTarget, m_Id);
            AssertNoGlError();
            fixed (void* ptr = &data[0])
            {
                glBufferData(BindTarget, SizeOf<T>(data.Length), ptr, (int)usageHint);
                AssertNoGlError();
            }
        }

        m_IsAllocated = true;
    }
    
    // NOTE(Zee): This API may not be the best, but it works for now
    public void MapWrite(int offset, int length, Action<GpuMemory<T>> writeFunc)
    {
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
    
    public void Free()
    {
        if (!m_IsAllocated)
            throw new Exception("Buffer is not allocated");
        
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