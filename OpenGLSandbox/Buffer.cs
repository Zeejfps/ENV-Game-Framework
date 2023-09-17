using static OpenGL.Gl;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed unsafe class Buffer<T> : IDisposable where T : unmanaged
{
    private readonly int m_Target;
    private readonly int m_BufferSize;
    private readonly void* m_BufferPtr;

    private int m_Index;
    private bool m_IsDisposed;

    public static Buffer<T> Allocate(int target, int size, int usage)
    {
        glBufferData(target, size * sizeof(T), IntPtr.Zero, usage);
        return new Buffer<T>(target, size);
    }
    
    public Buffer(int target, int size)
    {
        m_Target = target;
        
        m_BufferPtr = (void*)glMapBuffer(m_Target, GL_WRITE_ONLY);
        AssertNoGlError();
        
        m_BufferSize = size;
    }

    public void Dispose()
    {
        if (m_IsDisposed)
            return;
        
        m_IsDisposed = true;
        glUnmapBuffer(m_Target);
        AssertNoGlError();
    }

    public void Write(T data)
    {
        if (m_IsDisposed)
            throw new Exception("Is Disposed!");

        if (m_Index >= m_BufferSize)
            throw new Exception("Buffer capacity reached!");
        
        var buffer = new Span<T>(m_BufferPtr, m_BufferSize);
        buffer[m_Index] = data;
        m_Index++;
    }
}