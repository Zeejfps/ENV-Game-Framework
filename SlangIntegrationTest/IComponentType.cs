using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

[Guid("5bc42be8-5c50-4929-9e5e-d15e7c24015f")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IComponentType : ISlangUnknown
{
    [PreserveSig]
    IntPtr GetSession();

    [PreserveSig]
    IntPtr GetLayout(
        int targetIndex,
        out IntPtr outDiagnostics);

    [PreserveSig]
    int GetSpecializationParamCount();

    [PreserveSig]
    int GetEntryPointCode(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outCode,
        out IntPtr outDiagnostics);

    [PreserveSig]
    int GetResultAsFileSystem(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outFileSystem);

    [PreserveSig]
    void GetEntryPointHash(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outHash);

    [PreserveSig]
    int Specialize(
        IntPtr specializationArgs,
        int specializationArgCount,
        out IntPtr outSpecializedComponentType,
        out IntPtr outDiagnostics);

    [PreserveSig]
    int Link(
        out IntPtr outLinkedComponentType,
        out IntPtr outDiagnostics);

    [PreserveSig]
    int GetEntryPointHostCallable(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outSharedLibrary,
        out IntPtr outDiagnostics);

    [PreserveSig]
    int RenameEntryPoint(
        [MarshalAs(UnmanagedType.LPStr)] string newName,
        out IntPtr outEntryPoint);

    [PreserveSig]
    int LinkWithOptions(
        out IntPtr outLinkedComponentType,
        uint compilerOptionEntryCount,
        IntPtr compilerOptionEntries,
        out IntPtr outDiagnostics);

    [PreserveSig]
    int GetTargetCode(
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