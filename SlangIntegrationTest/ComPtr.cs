using System.Runtime.InteropServices;
using SlangIntegrationTest;

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
        return (T)SlangCompilerAPI.ComWrappers.GetOrCreateObjectForComInstance(m_Ptr, CreateObjectFlags.None);
    }

    public static implicit operator IntPtr(ComPtr<T> comPtr) => comPtr.m_Ptr;

    public override string ToString()
    {
        return m_Ptr.ToString();
    }
}
