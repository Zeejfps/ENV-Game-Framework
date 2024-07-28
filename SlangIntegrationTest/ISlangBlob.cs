using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

[Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ISlangBlob
{
    [PreserveSig]
    IntPtr GetBufferPointer();
        
    [PreserveSig]
    IntPtr  GetBufferSize();
}