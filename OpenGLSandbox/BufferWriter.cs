using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

/// <summary>
/// A helpful abstraction for using mapped buffers and writing data to the buffer
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed unsafe class BufferWriter<T> : IDisposable where T : unmanaged
{
    private readonly int m_Target;
    private readonly int m_BufferSize;
    private IntPtr m_BufferPtr;

    private int m_Index;
    private bool m_IsDisposed;

    // NOTE(Zee): This is doable but bad. If I don't use ref on T data it gets copied
    // The copy could be a lot of data. I rather explicitly use openGL to upload it
    // public static void Upload(int target, T data, int usage)
    // {
    //     void* ptr = &data;
    //     glBufferData(target, new IntPtr(sizeof(T)), ptr, usage);
    // }
    
    public static BufferWriter<T> AllocateAndMap(int target, int size, int usage)
    {
        glBufferData(target, new IntPtr(size * sizeof(T)), (void*)0, usage);
        return new BufferWriter<T>(target, size);
    }
    
    public static BufferWriter<T> Map(int target, int size)
    {
        return new BufferWriter<T>(target, size);
    }
    
    public BufferWriter(int target, int size)
    {
        m_Target = target;
        
        m_BufferPtr = new IntPtr(glMapBuffer(m_Target, GL_WRITE_ONLY));
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

    // NOTE(Zee): I am not using the 'in' keyword here because C# creates a 
    // defensive copy if the struct is not a readonly struct
    public void Write(ref T data)
    {
        if (m_IsDisposed)
            throw new Exception("Is Disposed!");

        if (m_Index >= m_BufferSize)
            throw new Exception("Buffer capacity reached!");
        
        var buffer = new Span<T>((void*)m_BufferPtr, m_BufferSize);
        buffer[m_Index] = data;
        m_Index++;
    }
    
    public void Write(T data)
    {
        Write(ref data);
    }

    public void Write(Span<T> data)
    {
        if (m_IsDisposed)
            throw new Exception("Is Disposed!");

        if (m_Index >= m_BufferSize)
            throw new Exception("Buffer capacity reached!");

        if (m_Index + data.Length > m_BufferSize)
            throw new Exception("This will overflow the buffer!");
        
        var buffer = new Span<T>((void*)m_BufferPtr, m_BufferSize);
        data.CopyTo(buffer);
        m_BufferPtr += data.Length;
        m_Index += data.Length;
    }
}