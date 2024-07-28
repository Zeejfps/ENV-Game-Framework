using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

[Guid("00000000-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ISlangUnknown
{
    [PreserveSig]
    int QueryInterface(ref Guid guid, out IntPtr outObject);

    [PreserveSig]
    uint AddRef();

    [PreserveSig]
    uint Release();
}