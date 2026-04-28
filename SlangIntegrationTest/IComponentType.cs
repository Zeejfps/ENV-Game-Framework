using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SlangIntegrationTest;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
[Guid("5bc42be8-5c50-4929-9e5e-d15e7c24015f")]
public partial interface IComponentType
{
    [PreserveSig] IntPtr GetSession();

    [PreserveSig] IntPtr GetLayout(long targetIndex, out ISlangBlob? outDiagnostics);

    [PreserveSig] long GetSpecializationParamCount();

    [PreserveSig] int GetEntryPointCode(long entryPointIndex, long targetIndex, out ISlangBlob? outCode, out ISlangBlob? outDiagnostics);

    [PreserveSig] int GetResultAsFileSystem(long entryPointIndex, long targetIndex, out IntPtr outFileSystem);

    [PreserveSig] void GetEntryPointHash(long entryPointIndex, long targetIndex, out ISlangBlob? outHash);

    [PreserveSig] int Specialize(IntPtr specializationArgs, long specializationArgCount, out IntPtr outSpecializedComponentType, out ISlangBlob? outDiagnostics);

    [PreserveSig] int Link(out IntPtr outLinkedComponentType, out ISlangBlob? outDiagnostics);

    [PreserveSig] int GetEntryPointHostCallable(int entryPointIndex, int targetIndex, out IntPtr outSharedLibrary, out ISlangBlob? outDiagnostics);

    [PreserveSig] int RenameEntryPoint(string newName, out IntPtr outEntryPoint);

    [PreserveSig] int LinkWithOptions(out IntPtr outLinkedComponentType, uint compilerOptionEntryCount, IntPtr compilerOptionEntries, out ISlangBlob? outDiagnostics);

    [PreserveSig] int GetTargetCode(long targetIndex, out ISlangBlob? outCode, out ISlangBlob? outDiagnostics);

    [PreserveSig] int GetTargetMetadata(long targetIndex, out IntPtr outMetadata, out ISlangBlob? outDiagnostics);

    [PreserveSig] int GetEntryPointMetadata(long entryPointIndex, long targetIndex, out IntPtr outMetadata, out ISlangBlob? outDiagnostics);
}
