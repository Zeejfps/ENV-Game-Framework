using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SlangIntegrationTest;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
[Guid("67618701-d116-468f-ab3b-474bedce0e3d")]
public partial interface ISession
{
    IGlobalSession GetGlobalSession();

    IntPtr LoadModule(
        string moduleName,
        out ISlangBlob outDiagnostics);

    IntPtr LoadModuleFromSource(
        string moduleName,
        string path,
        IntPtr source,
        out IntPtr outDiagnostics);

    int CreateCompositeComponentType(
        IntPtr componentTypes,
        int componentTypeCount,
        out IntPtr outCompositeComponentType,
        out IntPtr outDiagnostics);

    IntPtr SpecializeType(
        IntPtr type,
        IntPtr specializationArgs,
        int specializationArgCount,
        out IntPtr outDiagnostics);

    IntPtr GetTypeLayout(
        IntPtr type,
        int targetIndex,
        LayoutRules rules,
        out IntPtr outDiagnostics);

    IntPtr GetContainerType(
        IntPtr elementType,
        ContainerType containerType,
        out IntPtr outDiagnostics);

    IntPtr GetDynamicType();

    int GetTypeRTTIMangledName(IntPtr type, out IntPtr outNameBlob);

    int GetTypeConformanceWitnessMangledName(
        IntPtr type,
        IntPtr interfaceType,
        out IntPtr outNameBlob);

    int GetTypeConformanceWitnessSequentialID(
        IntPtr type,
        IntPtr interfaceType,
        out uint outId);

    int CreateCompileRequest(out IntPtr outCompileRequest);

    int CreateTypeConformanceComponentType(
        IntPtr type,
        IntPtr interfaceType,
        out IntPtr outConformance,
        int conformanceIdOverride,
        out IntPtr outDiagnostics);

    IntPtr LoadModuleFromIRBlob(
        string moduleName,
        string path,
        IntPtr source,
        out IntPtr outDiagnostics);

    int GetLoadedModuleCount();

    IntPtr GetLoadedModule(int index);

    [return: MarshalAs(UnmanagedType.U1)]
    bool IsBinaryModuleUpToDate(
        string modulePath,
        IntPtr binaryModuleBlob);

    IntPtr LoadModuleFromSourceString(
        string moduleName,
        string path,
        string source,
        out IntPtr outDiagnostics);
}
