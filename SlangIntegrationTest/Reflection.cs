// Slang reflection wrappers — non-owning views into IComponentType-owned memory.
//
// LIFETIME: Every handle below (ProgramLayout, EntryPointReflection, TypeReflection,
// TypeLayoutReflection, VariableReflection, VariableLayoutReflection) is owned by the
// IComponentType that produced the ProgramLayout. Releasing that component (Marshal.Release
// on the linked program pointer, or letting it go out of scope and being collected)
// invalidates EVERY derived handle. Finish all reflection traversal before releasing the
// component. These wrappers intentionally do not implement IDisposable — they are views,
// not owners.

using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

public readonly record struct ProgramLayout(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public uint ParameterCount => SlangNative.spReflection_GetParameterCount(Handle);

    public ulong EntryPointCount => SlangNative.spReflection_getEntryPointCount(Handle);

    public ulong GlobalConstantBufferBinding =>
        SlangNative.spReflection_getGlobalConstantBufferBinding(Handle);

    public nuint GlobalConstantBufferSize =>
        SlangNative.spReflection_getGlobalConstantBufferSize(Handle);

    public TypeLayoutReflection GlobalParamsTypeLayout =>
        new(SlangNative.spReflection_getGlobalParamsTypeLayout(Handle));

    public VariableLayoutReflection GlobalParamsVarLayout =>
        new(SlangNative.spReflection_getGlobalParamsVarLayout(Handle));

    // Returns the underlying ISession*. Out of reflection scope — kept as raw IntPtr.
    public IntPtr Session => SlangNative.spReflection_GetSession(Handle);

    public VariableLayoutReflection GetParameter(uint index) =>
        new(SlangNative.spReflection_GetParameterByIndex(Handle, index));

    public EntryPointReflection GetEntryPointByIndex(ulong index) =>
        new(SlangNative.spReflection_getEntryPointByIndex(Handle, index));

    public EntryPointReflection FindEntryPointByName(string name) =>
        new(SlangNative.spReflection_findEntryPointByName(Handle, name));

    public TypeReflection FindTypeByName(string name) =>
        new(SlangNative.spReflection_FindTypeByName(Handle, name));

    public TypeLayoutReflection GetTypeLayout(TypeReflection type, SlangLayoutRules rules) =>
        new(SlangNative.spReflection_GetTypeLayout(Handle, type.Handle, rules));
}

public readonly record struct EntryPointReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public string? Name =>
        Marshal.PtrToStringUTF8(SlangNative.spReflectionEntryPoint_getName(Handle));

    public string? NameOverride =>
        Marshal.PtrToStringUTF8(SlangNative.spReflectionEntryPoint_getNameOverride(Handle));

    public SlangStage Stage => SlangNative.spReflectionEntryPoint_getStage(Handle);

    public uint ParameterCount => SlangNative.spReflectionEntryPoint_getParameterCount(Handle);

    public bool HasDefaultConstantBuffer =>
        SlangNative.spReflectionEntryPoint_hasDefaultConstantBuffer(Handle) != 0;

    public bool UsesAnySampleRateInput =>
        SlangNative.spReflectionEntryPoint_usesAnySampleRateInput(Handle) != 0;

    public VariableLayoutReflection VarLayout =>
        new(SlangNative.spReflectionEntryPoint_getVarLayout(Handle));

    public VariableLayoutReflection ResultVarLayout =>
        new(SlangNative.spReflectionEntryPoint_getResultVarLayout(Handle));

    public VariableLayoutReflection GetParameterByIndex(uint index) =>
        new(SlangNative.spReflectionEntryPoint_getParameterByIndex(Handle, index));

    public unsafe (ulong X, ulong Y, ulong Z) GetComputeThreadGroupSize()
    {
        ulong* axes = stackalloc ulong[3];
        SlangNative.spReflectionEntryPoint_getComputeThreadGroupSize(Handle, 3, axes);
        return (axes[0], axes[1], axes[2]);
    }

    public unsafe ulong GetComputeWaveSize()
    {
        ulong waveSize = 0;
        SlangNative.spReflectionEntryPoint_getComputeWaveSize(Handle, &waveSize);
        return waveSize;
    }
}

public readonly record struct TypeReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public SlangTypeKind Kind => SlangNative.spReflectionType_GetKind(Handle);

    public string? Name =>
        Marshal.PtrToStringUTF8(SlangNative.spReflectionType_GetName(Handle));

    public uint FieldCount => SlangNative.spReflectionType_GetFieldCount(Handle);

    public nuint ElementCount => SlangNative.spReflectionType_GetElementCount(Handle);

    public TypeReflection ElementType =>
        new(SlangNative.spReflectionType_GetElementType(Handle));

    public uint RowCount => SlangNative.spReflectionType_GetRowCount(Handle);

    public uint ColumnCount => SlangNative.spReflectionType_GetColumnCount(Handle);

    public SlangScalarType ScalarType => SlangNative.spReflectionType_GetScalarType(Handle);

    public SlangResourceShape ResourceShape => SlangNative.spReflectionType_GetResourceShape(Handle);

    public SlangResourceAccess ResourceAccess => SlangNative.spReflectionType_GetResourceAccess(Handle);

    public TypeReflection ResourceResultType =>
        new(SlangNative.spReflectionType_GetResourceResultType(Handle));

    public VariableReflection GetFieldByIndex(uint index) =>
        new(SlangNative.spReflectionType_GetFieldByIndex(Handle, index));
}

public readonly record struct TypeLayoutReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public TypeReflection Type => new(SlangNative.spReflectionTypeLayout_GetType(Handle));

    public SlangTypeKind Kind => SlangNative.spReflectionTypeLayout_getKind(Handle);

    public uint FieldCount => SlangNative.spReflectionTypeLayout_GetFieldCount(Handle);

    public TypeLayoutReflection ElementTypeLayout =>
        new(SlangNative.spReflectionTypeLayout_GetElementTypeLayout(Handle));

    public VariableLayoutReflection ElementVarLayout =>
        new(SlangNative.spReflectionTypeLayout_GetElementVarLayout(Handle));

    public VariableLayoutReflection ContainerVarLayout =>
        new(SlangNative.spReflectionTypeLayout_getContainerVarLayout(Handle));

    public SlangParameterCategory ParameterCategory =>
        SlangNative.spReflectionTypeLayout_GetParameterCategory(Handle);

    public uint CategoryCount => SlangNative.spReflectionTypeLayout_GetCategoryCount(Handle);

    public SlangMatrixLayoutMode MatrixLayoutMode =>
        SlangNative.spReflectionTypeLayout_GetMatrixLayoutMode(Handle);

    public nuint GetSize(SlangParameterCategory category = SlangParameterCategory.Uniform) =>
        SlangNative.spReflectionTypeLayout_GetSize(Handle, category);

    public nuint GetStride(SlangParameterCategory category = SlangParameterCategory.Uniform) =>
        SlangNative.spReflectionTypeLayout_GetStride(Handle, category);

    public int GetAlignment(SlangParameterCategory category = SlangParameterCategory.Uniform) =>
        SlangNative.spReflectionTypeLayout_getAlignment(Handle, category);

    public nuint GetElementStride(SlangParameterCategory category) =>
        SlangNative.spReflectionTypeLayout_GetElementStride(Handle, category);

    public VariableLayoutReflection GetFieldByIndex(uint index) =>
        new(SlangNative.spReflectionTypeLayout_GetFieldByIndex(Handle, index));

    public SlangParameterCategory GetCategoryByIndex(uint index) =>
        SlangNative.spReflectionTypeLayout_GetCategoryByIndex(Handle, index);
}

public readonly record struct VariableReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public string? Name =>
        Marshal.PtrToStringUTF8(SlangNative.spReflectionVariable_GetName(Handle));

    public TypeReflection Type => new(SlangNative.spReflectionVariable_GetType(Handle));
}

public readonly record struct VariableLayoutReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public VariableReflection Variable =>
        new(SlangNative.spReflectionVariableLayout_GetVariable(Handle));

    public TypeLayoutReflection TypeLayout =>
        new(SlangNative.spReflectionVariableLayout_GetTypeLayout(Handle));

    public SlangImageFormat ImageFormat =>
        SlangNative.spReflectionVariableLayout_GetImageFormat(Handle);

    public string? SemanticName =>
        Marshal.PtrToStringUTF8(SlangNative.spReflectionVariableLayout_GetSemanticName(Handle));

    public nuint SemanticIndex =>
        SlangNative.spReflectionVariableLayout_GetSemanticIndex(Handle);

    public SlangStage Stage => SlangNative.spReflectionVariableLayout_getStage(Handle);

    public nuint GetOffset(SlangParameterCategory category) =>
        SlangNative.spReflectionVariableLayout_GetOffset(Handle, category);

    public nuint GetSpace(SlangParameterCategory category) =>
        SlangNative.spReflectionVariableLayout_GetSpace(Handle, category);
}
