using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper.Buffers;

internal sealed class BufferMemoryRange<T> : IReadWriteBufferMemoryRange<T> where T : unmanaged
{
    public Span<T> Span
    {
        get
        {
            unsafe
            {
                return new Span<T>(m_Ptr, Count);
            }
        }
    }
    
    private readonly uint m_BufferKind;
    private int m_Offset;
    private unsafe void* m_Ptr;
    private uint m_AccessFlag;
    public int Count { get; private set; }

    public unsafe BufferMemoryRange(uint bufferKind, int offset, void* ptr, int length, uint mappedBufferAccessFlag)
    {
        m_BufferKind = bufferKind;
        m_Offset = offset;
        m_Ptr = ptr;
        Count = length;
        m_AccessFlag = mappedBufferAccessFlag;
    }
    
    public void Write(int index, T data)
    {
        if (index < 0 || index >= Count)
            throw new IndexOutOfRangeException();
        
        var span = Span;
        span[index] = data;
    }

    public T Read(int index)
    {
        if (index < 0 || index >= Count)
            throw new IndexOutOfRangeException();
        
        return Span[index];
    }

    public void Flush()
    {
        if ((m_AccessFlag & GL_MAP_FLUSH_EXPLICIT_BIT) == 0)
            throw new InvalidOperationException("Can't flush buffer that does not have the FlushExplicit access flag set");
        
        glFlushMappedBufferRange(GL_ARRAY_BUFFER, SizeOf<T>(m_Offset), SizeOf<T>(Count));
        AssertNoGlError();
    }

    public void Dispose()
    {
        unsafe
        {
            m_Offset = 0;
            m_Ptr = (void*)0;
            Count = 0;
            glUnmapBuffer(m_BufferKind);
            OpenGlUtils.AssertNoGlError();
        }
    }
}