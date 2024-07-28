using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

[Guid("67618701-d116-468f-ab3b-474bedce0e3d")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ISession
{
    [PreserveSig]
    IGlobalSession GetGlobalSession();

    IModule? LoadModule(
        [MarshalAs(UnmanagedType.LPStr)] string moduleName,
        [Out] out ISlangBlob? outDiagnostics);

    IModule LoadModuleFromSource(
        [MarshalAs(UnmanagedType.LPStr)] string moduleName,
        [MarshalAs(UnmanagedType.LPStr)] string path,
        ISlangBlob source,
        [Out] out ISlangBlob outDiagnostics);

    [PreserveSig]
    int CreateCompositeComponentType(
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IComponentType[] componentTypes,
        int componentTypeCount,
        out IComponentType outCompositeComponentType,
        [Out] out ISlangBlob outDiagnostics);

    TypeReflection SpecializeType(
        TypeReflection type,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] SpecializationArg[] specializationArgs,
        int specializationArgCount,
        [Out] out ISlangBlob outDiagnostics);

    TypeLayoutReflection GetTypeLayout(
        TypeReflection type,
        int targetIndex,
        LayoutRules rules,
        [Out] out ISlangBlob outDiagnostics);

    TypeReflection GetContainerType(
        TypeReflection elementType,
        ContainerType containerType,
        [Out] out ISlangBlob outDiagnostics);

    TypeReflection GetDynamicType();

    [PreserveSig]
    int GetTypeRTTIMangledName(
        TypeReflection type,
        [Out] out ISlangBlob outNameBlob);

    [PreserveSig]
    int GetTypeConformanceWitnessMangledName(
        TypeReflection type,
        TypeReflection interfaceType,
        [Out] out ISlangBlob outNameBlob);

    [PreserveSig]
    int GetTypeConformanceWitnessSequentialID(
        TypeReflection type,
        TypeReflection interfaceType,
        out uint outId);

    [PreserveSig]
    int CreateCompileRequest(
        out SlangCompileRequest outCompileRequest);

    [PreserveSig]
    int CreateTypeConformanceComponentType(
        TypeReflection type,
        TypeReflection interfaceType,
        out ITypeConformance outConformance,
        int conformanceIdOverride,
        [Out] out ISlangBlob outDiagnostics);

    IModule LoadModuleFromIRBlob(
        [MarshalAs(UnmanagedType.LPStr)] string moduleName,
        [MarshalAs(UnmanagedType.LPStr)] string path,
        ISlangBlob source,
        [Out] out ISlangBlob outDiagnostics);

    int GetLoadedModuleCount();

    IModule GetLoadedModule(int index);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool IsBinaryModuleUpToDate(
        [MarshalAs(UnmanagedType.LPStr)] string modulePath,
        ISlangBlob binaryModuleBlob);

    IModule LoadModuleFromSourceString(
        [MarshalAs(UnmanagedType.LPStr)] string moduleName,
        [MarshalAs(UnmanagedType.LPStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string source,
        out ISlangBlob outDiagnostics);
}