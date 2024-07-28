using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

[Guid("0c720e64-8722-4d31-8990-638a98b1c279")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IModule
{
    [PreserveSig]
    int FindEntryPointByName(
        [MarshalAs(UnmanagedType.LPStr)] string name,
        out IntPtr outEntryPoint);

    [PreserveSig]
    int GetDefinedEntryPointCount();

    [PreserveSig]
    int GetDefinedEntryPoint(int index, out IntPtr outEntryPoint);

    [PreserveSig]
    int Serialize(out IntPtr outSerializedBlob);

    [PreserveSig]
    int WriteToFile([MarshalAs(UnmanagedType.LPStr)] string fileName);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.LPStr)]
    string GetName();

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.LPStr)]
    string GetFilePath();

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.LPStr)]
    string GetUniqueIdentity();

    [PreserveSig]
    int FindAndCheckEntryPoint(
        [MarshalAs(UnmanagedType.LPStr)] string name,
        int stage,
        out IntPtr outEntryPoint,
        out IntPtr outDiagnostics);

    [PreserveSig]
    int GetDependencyFileCount();

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.LPStr)]
    string GetDependencyFilePath(int index);

    [PreserveSig]
    IntPtr GetModuleReflection();
}