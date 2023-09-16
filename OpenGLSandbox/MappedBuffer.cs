using OpenGL;

namespace OpenGLSandbox;

public sealed class MappedBuffer<T> : IDisposable where T : struct
{
    private readonly int m_Target;
    private readonly int m_BufferSize;
    private readonly unsafe void* m_BufferPtr;

    private int m_Index;
    private bool m_IsDisposed;

    public unsafe MappedBuffer(int target, int size)
    {
        m_Target = target;
        
        m_BufferPtr = (void*)Gl.glMapBuffer(m_Target, Gl.GL_WRITE_ONLY);
        Utils_GL.AssertNoGlError();
        
        m_BufferSize = size;
    }

    public void Dispose()
    {
        if (m_IsDisposed)
            return;
        
        m_IsDisposed = true;
        Gl.glUnmapBuffer(m_Target);
        Utils_GL.AssertNoGlError();
    }

    public unsafe void Write(T data)
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