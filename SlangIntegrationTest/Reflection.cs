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
using System.Text;

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

    public uint UserAttributeCount => SlangNative.spReflectionType_GetUserAttributeCount(Handle);

    public UserAttributeReflection GetUserAttribute(uint index) =>
        new(SlangNative.spReflectionType_GetUserAttribute(Handle, index));

    public UserAttributeReflection FindUserAttributeByName(string name) =>
        new(SlangNative.spReflectionType_FindUserAttributeByName(Handle, name));
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

    public unsafe long FindFieldIndexByName(string name)
    {
        var byteCount = Encoding.UTF8.GetByteCount(name);
        Span<byte> buf = stackalloc byte[byteCount];
        Encoding.UTF8.GetBytes(name, buf);
        fixed (byte* p = buf)
            return SlangNative.spReflectionTypeLayout_findFieldIndexByName(Handle, p, p + byteCount);
    }

    public VariableLayoutReflection ExplicitCounter =>
        new(SlangNative.spReflectionTypeLayout_GetExplicitCounter(Handle));

    public long GetFieldBindingRangeOffset(long fieldIndex) =>
        SlangNative.spReflectionTypeLayout_getFieldBindingRangeOffset(Handle, fieldIndex);

    public long ExplicitCounterBindingRangeOffset =>
        SlangNative.spReflectionTypeLayout_getExplicitCounterBindingRangeOffset(Handle);

    // Binding ranges
    public long BindingRangeCount =>
        SlangNative.spReflectionTypeLayout_getBindingRangeCount(Handle);

    public SlangBindingType GetBindingRangeType(long index) =>
        SlangNative.spReflectionTypeLayout_getBindingRangeType(Handle, index);

    public bool IsBindingRangeSpecializable(long index) =>
        SlangNative.spReflectionTypeLayout_isBindingRangeSpecializable(Handle, index) != 0;

    /// <summary>Number of bindings; -1 indicates unbounded array.</summary>
    public long GetBindingRangeBindingCount(long index) =>
        SlangNative.spReflectionTypeLayout_getBindingRangeBindingCount(Handle, index);

    public long GetBindingRangeIndexOffset(long index) =>
        SlangNative.spReflectionTypeLayout_getBindingRangeIndexOffset(Handle, index);

    public long GetBindingRangeSpaceOffset(long index) =>
        SlangNative.spReflectionTypeLayout_getBindingRangeSpaceOffset(Handle, index);

    public SlangImageFormat GetBindingRangeImageFormat(long index) =>
        SlangNative.spReflectionTypeLayout_getBindingRangeImageFormat(Handle, index);

    public long GetBindingRangeDescriptorSetIndex(long index) =>
        SlangNative.spReflectionTypeLayout_getBindingRangeDescriptorSetIndex(Handle, index);

    public long GetBindingRangeFirstDescriptorRangeIndex(long index) =>
        SlangNative.spReflectionTypeLayout_getBindingRangeFirstDescriptorRangeIndex(Handle, index);

    public long GetBindingRangeDescriptorRangeCount(long index) =>
        SlangNative.spReflectionTypeLayout_getBindingRangeDescriptorRangeCount(Handle, index);

    public TypeLayoutReflection GetBindingRangeLeafTypeLayout(long index) =>
        new(SlangNative.spReflectionTypeLayout_getBindingRangeLeafTypeLayout(Handle, index));

    public VariableReflection GetBindingRangeLeafVariable(long index) =>
        new(SlangNative.spReflectionTypeLayout_getBindingRangeLeafVariable(Handle, index));

    // Descriptor sets (Vulkan-style descriptor set introspection)
    public long DescriptorSetCount =>
        SlangNative.spReflectionTypeLayout_getDescriptorSetCount(Handle);

    public long GetDescriptorSetSpaceOffset(long setIndex) =>
        SlangNative.spReflectionTypeLayout_getDescriptorSetSpaceOffset(Handle, setIndex);

    public long GetDescriptorSetDescriptorRangeCount(long setIndex) =>
        SlangNative.spReflectionTypeLayout_getDescriptorSetDescriptorRangeCount(Handle, setIndex);

    public long GetDescriptorSetDescriptorRangeIndexOffset(long setIndex, long rangeIndex) =>
        SlangNative.spReflectionTypeLayout_getDescriptorSetDescriptorRangeIndexOffset(Handle, setIndex, rangeIndex);

    public long GetDescriptorSetDescriptorRangeDescriptorCount(long setIndex, long rangeIndex) =>
        SlangNative.spReflectionTypeLayout_getDescriptorSetDescriptorRangeDescriptorCount(Handle, setIndex, rangeIndex);

    public SlangBindingType GetDescriptorSetDescriptorRangeType(long setIndex, long rangeIndex) =>
        SlangNative.spReflectionTypeLayout_getDescriptorSetDescriptorRangeType(Handle, setIndex, rangeIndex);

    public SlangParameterCategory GetDescriptorSetDescriptorRangeCategory(long setIndex, long rangeIndex) =>
        SlangNative.spReflectionTypeLayout_getDescriptorSetDescriptorRangeCategory(Handle, setIndex, rangeIndex);

    // Sub-object ranges (for parameter blocks / structured buffers of objects)
    public long SubObjectRangeCount =>
        SlangNative.spReflectionTypeLayout_getSubObjectRangeCount(Handle);

    public long GetSubObjectRangeBindingRangeIndex(long subObjectRangeIndex) =>
        SlangNative.spReflectionTypeLayout_getSubObjectRangeBindingRangeIndex(Handle, subObjectRangeIndex);

    public long GetSubObjectRangeSpaceOffset(long subObjectRangeIndex) =>
        SlangNative.spReflectionTypeLayout_getSubObjectRangeSpaceOffset(Handle, subObjectRangeIndex);

    public VariableLayoutReflection GetSubObjectRangeOffset(long subObjectRangeIndex) =>
        new(SlangNative.spReflectionTypeLayout_getSubObjectRangeOffset(Handle, subObjectRangeIndex));
}

public readonly record struct VariableReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public string? Name =>
        Marshal.PtrToStringUTF8(SlangNative.spReflectionVariable_GetName(Handle));

    public TypeReflection Type => new(SlangNative.spReflectionVariable_GetType(Handle));

    public uint UserAttributeCount => SlangNative.spReflectionVariable_GetUserAttributeCount(Handle);

    public UserAttributeReflection GetUserAttribute(uint index) =>
        new(SlangNative.spReflectionVariable_GetUserAttribute(Handle, index));

    /// <summary>Look up a user attribute by name. Requires the IGlobalSession pointer
    /// because Slang resolves attribute names through the session's name table.</summary>
    public UserAttributeReflection FindUserAttributeByName(IntPtr globalSession, string name) =>
        new(SlangNative.spReflectionVariable_FindUserAttributeByName(Handle, globalSession, name));

    public ModifierReflection FindModifier(SlangModifierID id) =>
        new(SlangNative.spReflectionVariable_FindModifier(Handle, id));

    public bool HasModifier(SlangModifierID id) => !FindModifier(id).IsNull;
}

public readonly record struct UserAttributeReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public string? Name =>
        Marshal.PtrToStringUTF8(SlangNative.spReflectionUserAttribute_GetName(Handle));

    public uint ArgumentCount => SlangNative.spReflectionUserAttribute_GetArgumentCount(Handle);

    public TypeReflection GetArgumentType(uint index) =>
        new(SlangNative.spReflectionUserAttribute_GetArgumentType(Handle, index));

    public unsafe bool TryGetIntArgument(uint index, out int value)
    {
        int v;
        var hr = SlangNative.spReflectionUserAttribute_GetArgumentValueInt(Handle, index, &v);
        value = v;
        return hr >= 0;
    }

    public unsafe bool TryGetFloatArgument(uint index, out float value)
    {
        float v;
        var hr = SlangNative.spReflectionUserAttribute_GetArgumentValueFloat(Handle, index, &v);
        value = v;
        return hr >= 0;
    }

    public unsafe string? GetStringArgument(uint index)
    {
        nuint size = 0;
        var ptr = SlangNative.spReflectionUserAttribute_GetArgumentValueString(Handle, index, &size);
        if (ptr == IntPtr.Zero) return null;
        return Encoding.UTF8.GetString(new ReadOnlySpan<byte>((void*)ptr, (int)size));
    }
}

/// <summary>Opaque modifier handle. The mere existence (non-null) indicates the modifier
/// is set on the variable; there are no further queries.</summary>
public readonly record struct ModifierReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;
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
