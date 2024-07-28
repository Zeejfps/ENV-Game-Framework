using System.Runtime.InteropServices;

public sealed class ComPtr<T>
{
    private IntPtr m_Ptr;

    public IntPtr Ptr => m_Ptr;
    
    public ref IntPtr WriteRef()
    {
        return ref m_Ptr;
    }

    public T Get()
    {
        return (T)Marshal.GetObjectForIUnknown(m_Ptr);
    }
    
    public static implicit operator IntPtr(ComPtr<T> comPtr) => comPtr.m_Ptr;

    public override string ToString()
    {
        return m_Ptr.ToString();
    }
}