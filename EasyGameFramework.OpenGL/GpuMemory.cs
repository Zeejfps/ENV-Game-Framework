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