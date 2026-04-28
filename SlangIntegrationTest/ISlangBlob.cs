using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SlangIntegrationTest;

[GeneratedComInterface]
[Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102")]
public partial interface ISlangBlob
{
    [PreserveSig] IntPtr GetBufferPointer();
    [PreserveSig] IntPtr GetBufferSize();
}
