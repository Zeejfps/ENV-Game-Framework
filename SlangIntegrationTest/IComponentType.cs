using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SlangIntegrationTest;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
[Guid("5bc42be8-5c50-4929-9e5e-d15e7c24015f")]
public partial interface IComponentType
{
    IntPtr GetSession();

    IntPtr GetLayout(int targetIndex, out IntPtr outDiagnostics);

    int GetSpecializationParamCount();

    int GetEntryPointCode(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outCode,
        out IntPtr outDiagnostics);

    int GetResultAsFileSystem(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outFileSystem);

    void GetEntryPointHash(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outHash);

    int Specialize(
        IntPtr specializationArgs,
        int specializationArgCount,
        out IntPtr outSpecializedComponentType,
        out IntPtr outDiagnostics);

    int Link(out IntPtr outLinkedComponentType, out IntPtr outDiagnostics);

    int GetEntryPointHostCallable(
        int entryPointIndex,
        int targetIndex,
        out IntPtr outSharedLibrary,
        out IntPtr outDiagnostics);

    int RenameEntryPoint(string newName, out IntPtr outEntryPoint);

    int LinkWithOptions(
        out IntPtr outLinkedComponentType,
        uint compilerOptionEntryCount,
        IntPtr compilerOptionEntries,
        out IntPtr outDiagnostics);

    int GetTargetCode(
        int targetIndex,
        out IntPtr outCode,
        out IntPtr outDiagnostics);
}
