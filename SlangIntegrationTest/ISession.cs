using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SlangIntegrationTest;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
[Guid("67618701-d116-468f-ab3b-474bedce0e3d")]
public partial interface ISession
{
    [PreserveSig] IGlobalSession GetGlobalSession();

    [PreserveSig] IntPtr LoadModule(string moduleName, out ISlangBlob? outDiagnostics);

    [PreserveSig] IntPtr LoadModuleFromSource(string moduleName, string path, IntPtr source, out ISlangBlob? outDiagnostics);

    [PreserveSig] int CreateCompositeComponentType(IntPtr componentTypes, long componentTypeCount, out IntPtr outCompositeComponentType, out ISlangBlob? outDiagnostics);

    [PreserveSig] IntPtr SpecializeType(IntPtr type, IntPtr specializationArgs, long specializationArgCount, out ISlangBlob? outDiagnostics);

    [PreserveSig] IntPtr GetTypeLayout(IntPtr type, long targetIndex, LayoutRules rules, out ISlangBlob? outDiagnostics);

    [PreserveSig] IntPtr GetContainerType(IntPtr elementType, ContainerType containerType, out ISlangBlob? outDiagnostics);

    [PreserveSig] IntPtr GetDynamicType();

    [PreserveSig] int GetTypeRTTIMangledName(IntPtr type, out ISlangBlob? outNameBlob);

    [PreserveSig] int GetTypeConformanceWitnessMangledName(IntPtr type, IntPtr interfaceType, out ISlangBlob? outNameBlob);

    [PreserveSig] int GetTypeConformanceWitnessSequentialID(IntPtr type, IntPtr interfaceType, out uint outId);

    [PreserveSig] int CreateCompileRequest(out IntPtr outCompileRequest);

    [PreserveSig] int CreateTypeConformanceComponentType(IntPtr type, IntPtr interfaceType, out IntPtr outConformance, long conformanceIdOverride, out ISlangBlob? outDiagnostics);

    [PreserveSig] IntPtr LoadModuleFromIRBlob(string moduleName, string path, IntPtr source, out ISlangBlob? outDiagnostics);

    [PreserveSig] long GetLoadedModuleCount();

    [PreserveSig] IntPtr GetLoadedModule(long index);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.U1)]
    bool IsBinaryModuleUpToDate(string modulePath, IntPtr binaryModuleBlob);

    [PreserveSig] IntPtr LoadModuleFromSourceString(string moduleName, string path, string source, out ISlangBlob? outDiagnostics);

    [PreserveSig] int GetDynamicObjectRTTIBytes(IntPtr type, IntPtr interfaceType, out uint outRTTIDataBuffer, uint bufferSizeInBytes);

    [PreserveSig] int LoadModuleInfoFromIRBlob(IntPtr source, out long outModuleVersion, out IntPtr outModuleCompilerVersion, out IntPtr outModuleName);

    [PreserveSig] int GetDeclSourceLocation(IntPtr decl, out IntPtr outLocation);
}
