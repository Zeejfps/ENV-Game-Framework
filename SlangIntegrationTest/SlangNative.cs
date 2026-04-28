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

    [LibraryImport(Lib)]
    public static partial uint spReflectionType_GetUserAttributeCount(IntPtr type);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionType_GetUserAttribute(IntPtr type, uint index);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr spReflectionType_FindUserAttributeByName(IntPtr type, string name);

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

    [LibraryImport(Lib)]
    public static unsafe partial long spReflectionTypeLayout_findFieldIndexByName(IntPtr typeLayout, byte* nameBegin, byte* nameEnd);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionTypeLayout_GetExplicitCounter(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getFieldBindingRangeOffset(IntPtr typeLayout, long fieldIndex);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getExplicitCounterBindingRangeOffset(IntPtr typeLayout);

    // Binding ranges
    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getBindingRangeCount(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial SlangBindingType spReflectionTypeLayout_getBindingRangeType(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_isBindingRangeSpecializable(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getBindingRangeBindingCount(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getBindingRangeIndexOffset(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getBindingRangeSpaceOffset(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial SlangImageFormat spReflectionTypeLayout_getBindingRangeImageFormat(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getBindingRangeDescriptorSetIndex(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getBindingRangeFirstDescriptorRangeIndex(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getBindingRangeDescriptorRangeCount(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionTypeLayout_getBindingRangeLeafTypeLayout(IntPtr typeLayout, long index);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionTypeLayout_getBindingRangeLeafVariable(IntPtr typeLayout, long index);

    // Descriptor sets
    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getDescriptorSetCount(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getDescriptorSetSpaceOffset(IntPtr typeLayout, long setIndex);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getDescriptorSetDescriptorRangeCount(IntPtr typeLayout, long setIndex);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getDescriptorSetDescriptorRangeIndexOffset(IntPtr typeLayout, long setIndex, long rangeIndex);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getDescriptorSetDescriptorRangeDescriptorCount(IntPtr typeLayout, long setIndex, long rangeIndex);

    [LibraryImport(Lib)]
    public static partial SlangBindingType spReflectionTypeLayout_getDescriptorSetDescriptorRangeType(IntPtr typeLayout, long setIndex, long rangeIndex);

    [LibraryImport(Lib)]
    public static partial SlangParameterCategory spReflectionTypeLayout_getDescriptorSetDescriptorRangeCategory(IntPtr typeLayout, long setIndex, long rangeIndex);

    // Sub-object ranges
    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getSubObjectRangeCount(IntPtr typeLayout);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getSubObjectRangeBindingRangeIndex(IntPtr typeLayout, long subObjectRangeIndex);

    [LibraryImport(Lib)]
    public static partial long spReflectionTypeLayout_getSubObjectRangeSpaceOffset(IntPtr typeLayout, long subObjectRangeIndex);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionTypeLayout_getSubObjectRangeOffset(IntPtr typeLayout, long subObjectRangeIndex);

    // ---- Variable (SlangReflectionVariable*) ----------------------------------------------

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionVariable_GetName(IntPtr variable);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionVariable_GetType(IntPtr variable);

    [LibraryImport(Lib)]
    public static partial uint spReflectionVariable_GetUserAttributeCount(IntPtr variable);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionVariable_GetUserAttribute(IntPtr variable, uint index);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr spReflectionVariable_FindUserAttributeByName(IntPtr variable, IntPtr globalSession, string name);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionVariable_FindModifier(IntPtr variable, SlangModifierID modifierID);

    // ---- UserAttribute (SlangReflectionUserAttribute*) ------------------------------------

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionUserAttribute_GetName(IntPtr attrib);

    [LibraryImport(Lib)]
    public static partial uint spReflectionUserAttribute_GetArgumentCount(IntPtr attrib);

    [LibraryImport(Lib)]
    public static partial IntPtr spReflectionUserAttribute_GetArgumentType(IntPtr attrib, uint index);

    [LibraryImport(Lib)]
    public static unsafe partial int spReflectionUserAttribute_GetArgumentValueInt(IntPtr attrib, uint index, int* outValue);

    [LibraryImport(Lib)]
    public static unsafe partial int spReflectionUserAttribute_GetArgumentValueFloat(IntPtr attrib, uint index, float* outValue);

    [LibraryImport(Lib)]
    public static unsafe partial IntPtr spReflectionUserAttribute_GetArgumentValueString(IntPtr attrib, uint index, nuint* outSize);

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
