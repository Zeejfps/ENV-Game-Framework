using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

[Guid("0c720e64-8722-4d31-8990-638a98b1c279")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IModule : IComponentType
{
    [PreserveSig]
    int FindEntryPointByName(
        [MarshalAs(UnmanagedType.LPStr)] string name,
        out IEntryPoint outEntryPoint);

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
    
    [PreserveSig]
    new IntPtr GetSession();

    [PreserveSig]
    new IntPtr GetLayout(
        int targetIndex,
        out IntPtr outDiagnostics);

    [PreserveSig]
    new int GetSpecializationParamCount();

    [PreserveSig]
    new int GetEntryPointCode(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outCode,
        out IntPtr outDiagnostics);

    [PreserveSig]
    new int GetResultAsFileSystem(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outFileSystem);

    [PreserveSig]
    new void GetEntryPointHash(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outHash);

    [PreserveSig]
    new int Specialize(
        IntPtr specializationArgs,
        int specializationArgCount,
        out IntPtr outSpecializedComponentType,
        out IntPtr outDiagnostics);

    [PreserveSig]
    new int Link(
        out IntPtr outLinkedComponentType,
        out IntPtr outDiagnostics);

    [PreserveSig]
    new int GetEntryPointHostCallable(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outSharedLibrary,
        out IntPtr outDiagnostics);

    [PreserveSig]
    new int RenameEntryPoint(
        [MarshalAs(UnmanagedType.LPStr)] string newName,
        out IntPtr outEntryPoint);

    [PreserveSig]
    new int LinkWithOptions(
        out IntPtr outLinkedComponentType,
        uint compilerOptionEntryCount,
        IntPtr compilerOptionEntries,
        out IntPtr outDiagnostics);

    [PreserveSig]
    new int GetTargetCode(
        int targetIndex,
        out IntPtr outCode,
        out IntPtr outDiagnostics);
    
    [PreserveSig]
    new int QueryInterface(ref Guid guid, out IntPtr outObject);

    [PreserveSig]
    new uint AddRef();

    [PreserveSig]
    new uint Release();
}