namespace OpenGlWrapper.Buffers;

internal sealed class BufferMemoryRange<T> : IReadWriteBufferMemory<T> where T : unmanaged
{
    public Span<T> Span
    {
        get
        {
            unsafe
            {
                return new Span<T>(m_Ptr, Length);
            }
        }
    }
    
    private readonly uint m_BufferKind;

    private int m_Offset;
    private unsafe void* m_Ptr;
    public int Length { get; private set; }

    public unsafe BufferMemoryRange(uint bufferKind, int offset, void* ptr, int length)
    {
        m_BufferKind = bufferKind;
        m_Offset = offset;
        m_Ptr = ptr;
        Length = length;
    }
    
    public void Write(int index, T data)
    {
        if (index < 0 || index >= Length)
            throw new IndexOutOfRangeException();
        
        var span = Span;
        span[index] = data;
    }

    public T Read(int index)
    {
        if (index < 0 || index >= Length)
            throw new IndexOutOfRangeException();
        
        return Span[index];
    }

    public void Flush()
    {
        GL46.glFlushMappedBufferRange(GL46.GL_ARRAY_BUFFER, new IntPtr(m_Offset), new IntPtr(Length));
    }

    public void Dispose()
    {
        unsafe
        {
            m_Offset = 0;
            m_Ptr = (void*)0;
            Length = 0;
            GL46.glUnmapBuffer(m_BufferKind);
            OpenGlUtils.AssertNoGlError();
        }
    }
}