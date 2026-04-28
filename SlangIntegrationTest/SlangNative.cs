using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

internal static partial class SlangNative
{
    private const string Lib = SlangCompilerAPI.LibraryName;

    // ---- ProgramLayout (SlangReflection*) -------------------------------------------------

    [LibraryImport(Lib)]
    public static partial uint spReflection_GetParameterCount(IntPtr reflection);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflection_GetParameterByIndex(IntPtr reflection, uint index);

    [LibraryImport(Lib)]
    public static partial ulong spReflection_getEntryPointCount(IntPtr reflection);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflection_getEntryPointByIndex(IntPtr reflection, ulong index);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr spReflection_findEntryPointByName(IntPtr reflection, string name);

    [LibraryImport(Lib)]
    public static partial ulong spReflection_getGlobalConstantBufferBinding(IntPtr reflection);

    [LibraryImport(Lib)]
    public static partial nuint spReflection_getGlobalConstantBufferSize(IntPtr reflection);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflection_GetSession(IntPtr reflection);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflection_getGlobalParamsTypeLayout(IntPtr reflection);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflection_getGlobalParamsVarLayout(IntPtr reflection);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr spReflection_FindTypeByName(IntPtr reflection, string name);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflection_GetTypeLayout(IntPtr reflection, IntPtr type, SlangLayoutRules rules);

    // ---- EntryPoint (SlangReflectionEntryPoint*) ------------------------------------------

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionEntryPoint_getName(IntPtr entryPoint);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionEntryPoint_getNameOverride(IntPtr entryPoint);

    [LibraryImport(Lib)]
    public static partial uint spReflectionEntryPoint_getParameterCount(IntPtr entryPoint);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionEntryPoint_getParameterByIndex(IntPtr entryPoint, uint index);

    [LibraryImport(Lib)]
    public static partial SlangStage spReflectionEntryPoint_getStage(IntPtr entryPoint);

    [LibraryImport(Lib)]
    public static unsafe partial void spReflectionEntryPoint_getComputeThreadGroupSize(
        IntPtr entryPoint, ulong axisCount, ulong* outSizeAlongAxis);

    [LibraryImport(Lib)]
    public static unsafe partial void spReflectionEntryPoint_getComputeWaveSize(
        IntPtr entryPoint, ulong* outWaveSize);

    [LibraryImport(Lib)]
    public static partial int spReflectionEntryPoint_usesAnySampleRateInput(IntPtr entryPoint);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionEntryPoint_getVarLayout(IntPtr entryPoint);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionEntryPoint_getResultVarLayout(IntPtr entryPoint);

    [LibraryImport(Lib)]
    public static partial int spReflectionEntryPoint_hasDefaultConstantBuffer(IntPtr entryPoint);

    // ---- Type (SlangReflectionType*) ------------------------------------------------------

    [LibraryImport(Lib)]
    public static partial SlangTypeKind spReflectionType_GetKind(IntPtr type);

    [LibraryImport(Lib)]
    public static partial uint spReflectionType_GetFieldCount(IntPtr type);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionType_GetFieldByIndex(IntPtr type, uint index);

    [LibraryImport(Lib)]
    public static partial nuint spReflectionType_GetElementCount(IntPtr type);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionType_GetElementType(IntPtr type);

    [LibraryImport(Lib)]
    public static partial uint spReflectionType_GetRowCount(IntPtr type);

    [LibraryImport(Lib)]
    public static partial uint spReflectionType_GetColumnCount(IntPtr type);

    [LibraryImport(Lib)]
    public static partial SlangScalarType spReflectionType_GetScalarType(IntPtr type);

    [LibraryImport(Lib)]
    public static partial SlangResourceShape spReflectionType_GetResourceShape(IntPtr type);

    [LibraryImport(Lib)]
    public static partial SlangResourceAccess spReflectionType_GetResourceAccess(IntPtr type);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionType_GetResourceResultType(IntPtr type);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionType_GetName(IntPtr type);

    // ---- TypeLayout (SlangReflectionTypeLayout*) ------------------------------------------

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionTypeLayout_GetType(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial SlangTypeKind spReflectionTypeLayout_getKind(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial nuint spReflectionTypeLayout_GetSize(IntPtr typeLayout, SlangParameterCategory category);

    [LibraryImport(Lib)]
    public static partial nuint spReflectionTypeLayout_GetStride(IntPtr typeLayout, SlangParameterCategory category);

    [LibraryImport(Lib)]
    public static partial int spReflectionTypeLayout_getAlignment(IntPtr typeLayout, SlangParameterCategory category);

    [LibraryImport(Lib)]
    public static partial uint spReflectionTypeLayout_GetFieldCount(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionTypeLayout_GetFieldByIndex(IntPtr typeLayout, uint index);

    [LibraryImport(Lib)]
    public static partial nuint spReflectionTypeLayout_GetElementStride(IntPtr typeLayout, SlangParameterCategory category);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionTypeLayout_GetElementTypeLayout(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionTypeLayout_GetElementVarLayout(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionTypeLayout_getContainerVarLayout(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial SlangParameterCategory spReflectionTypeLayout_GetParameterCategory(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial uint spReflectionTypeLayout_GetCategoryCount(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial SlangParameterCategory spReflectionTypeLayout_GetCategoryByIndex(IntPtr typeLayout, uint index);

    [LibraryImport(Lib)]
    public static partial SlangMatrixLayoutMode spReflectionTypeLayout_GetMatrixLayoutMode(IntPtr typeLayout);

    // ---- Variable (SlangReflectionVariable*) ----------------------------------------------

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionVariable_GetName(IntPtr variable);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionVariable_GetType(IntPtr variable);

    // ---- VariableLayout (SlangReflectionVariableLayout*) ----------------------------------

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionVariableLayout_GetVariable(IntPtr variableLayout);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionVariableLayout_GetTypeLayout(IntPtr variableLayout);

    [LibraryImport(Lib)]
    public static partial nuint spReflectionVariableLayout_GetOffset(IntPtr variableLayout, SlangParameterCategory category);

    [LibraryImport(Lib)]
    public static partial nuint spReflectionVariableLayout_GetSpace(IntPtr variableLayout, SlangParameterCategory category);

    [LibraryImport(Lib)]
    public static partial SlangImageFormat spReflectionVariableLayout_GetImageFormat(IntPtr variableLayout);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionVariableLayout_GetSemanticName(IntPtr variableLayout);

    [LibraryImport(Lib)]
    public static partial nuint spReflectionVariableLayout_GetSemanticIndex(IntPtr variableLayout);

    [LibraryImport(Lib)]
    public static partial SlangStage spReflectionVariableLayout_getStage(IntPtr variableLayout);
}
