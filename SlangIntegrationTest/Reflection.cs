// Slang reflection wrappers — non-owning views into IComponentType-owned memory.
//
// LIFETIME: Every handle below (ProgramLayout, EntryPointReflection, TypeReflection,
// TypeLayoutReflection, VariableReflection, VariableLayoutReflection, DeclReflection,
// FunctionReflection, GenericReflection, TypeParameterReflection,
// UserAttributeReflection, ModifierReflection) is owned by the IComponentType that
// produced the ProgramLayout. Releasing that component (Marshal.Release on the linked
// program pointer, or letting it go out of scope and being collected) invalidates EVERY
// derived handle. Finish all reflection traversal before releasing the component.
// These wrappers intentionally do not implement IDisposable — they are views, not owners.
//
// DELIBERATE GAPS: array-marshalling methods (TryResolveOverloadedFunction, specializeType,
// specializeGeneric, applySpecializations family, specializeWithArgTypes) and ToJson are
// not bound. Add when a concrete need arises.

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

    public uint TypeParameterCount => SlangNative.spReflection_GetTypeParameterCount(Handle);

    public TypeParameterReflection GetTypeParameterByIndex(uint index) =>
        new(SlangNative.spReflection_GetTypeParameterByIndex(Handle, index));

    public TypeParameterReflection FindTypeParameter(string name) =>
        new(SlangNative.spReflection_FindTypeParameter(Handle, name));

    public FunctionReflection FindFunctionByName(string name) =>
        new(SlangNative.spReflection_FindFunctionByName(Handle, name));

    public FunctionReflection FindFunctionByNameInType(TypeReflection type, string name) =>
        new(SlangNative.spReflection_FindFunctionByNameInType(Handle, type.Handle, name));

    public VariableReflection FindVarByNameInType(TypeReflection type, string name) =>
        new(SlangNative.spReflection_FindVarByNameInType(Handle, type.Handle, name));

    public bool IsSubType(TypeReflection subType, TypeReflection superType) =>
        SlangNative.spReflection_isSubType(Handle, subType.Handle, superType.Handle) != 0;

    public ulong HashedStringCount => SlangNative.spReflection_getHashedStringCount(Handle);

    public unsafe string? GetHashedString(ulong index)
    {
        nuint count = 0;
        var ptr = SlangNative.spReflection_getHashedString(Handle, index, &count);
        if (ptr == IntPtr.Zero) return null;
        return Encoding.UTF8.GetString(new ReadOnlySpan<byte>((void*)ptr, (int)count));
    }

    public long BindlessSpaceIndex =>
        SlangNative.spReflection_getBindlessSpaceIndex(Handle);
}

public static class Slang
{
    /// <summary>Compute the same hash Slang uses for hashed-string lookup.</summary>
    public static unsafe uint ComputeStringHash(string text)
    {
        var byteCount = Encoding.UTF8.GetByteCount(text);
        Span<byte> buf = stackalloc byte[byteCount];
        Encoding.UTF8.GetBytes(text, buf);
        fixed (byte* p = buf)
            return SlangNative.spComputeStringHash(p, (nuint)byteCount);
    }
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

    public FunctionReflection Function =>
        new(SlangNative.spReflectionEntryPoint_getFunction(Handle));
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

    /// <summary>Returns the fully-qualified name (e.g. <c>Slang.MyStruct</c>) by reading
    /// from a Slang-allocated blob. Returns null if Slang reports failure.</summary>
    public unsafe string? GetFullName()
    {
        var hr = SlangNative.spReflectionType_GetFullName(Handle, out var blobPtr);
        if (hr < 0 || blobPtr == IntPtr.Zero) return null;
        var blob = (ISlangBlob)SlangCompilerAPI.ComWrappers
            .GetOrCreateObjectForComInstance(blobPtr, CreateObjectFlags.None);
        var bufPtr = blob.GetBufferPointer();
        var size = (int)blob.GetBufferSize();
        var name = Encoding.UTF8.GetString(new ReadOnlySpan<byte>((void*)bufPtr, size));
        Marshal.Release(blobPtr);
        return name;
    }

    public nuint GetSpecializedElementCount(ProgramLayout layout) =>
        SlangNative.spReflectionType_GetSpecializedElementCount(Handle, layout.Handle);

    public GenericReflection GenericContainer =>
        new(SlangNative.spReflectionType_GetGenericContainer(Handle));

    public long SpecializedTypeArgCount =>
        SlangNative.spReflectionType_getSpecializedTypeArgCount(Handle);

    public TypeReflection GetSpecializedTypeArg(long index) =>
        new(SlangNative.spReflectionType_getSpecializedTypeArgType(Handle, index));
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

    public int GenericParamIndex =>
        SlangNative.spReflectionTypeLayout_getGenericParamIndex(Handle);

    public TypeLayoutReflection PendingDataTypeLayout =>
        new(SlangNative.spReflectionTypeLayout_getPendingDataTypeLayout(Handle));

    public VariableLayoutReflection SpecializedTypePendingDataVarLayout =>
        new(SlangNative.spReflectionTypeLayout_getSpecializedTypePendingDataVarLayout(Handle));
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

    public bool HasDefaultValue =>
        SlangNative.spReflectionVariable_HasDefaultValue(Handle) != 0;

    public unsafe bool TryGetDefaultValueInt(out long value)
    {
        long v;
        var hr = SlangNative.spReflectionVariable_GetDefaultValueInt(Handle, &v);
        value = v;
        return hr >= 0;
    }

    public unsafe bool TryGetDefaultValueFloat(out float value)
    {
        float v;
        var hr = SlangNative.spReflectionVariable_GetDefaultValueFloat(Handle, &v);
        value = v;
        return hr >= 0;
    }

    public GenericReflection GenericContainer =>
        new(SlangNative.spReflectionVariable_GetGenericContainer(Handle));
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

    public VariableLayoutReflection PendingDataLayout =>
        new(SlangNative.spReflectionVariableLayout_getPendingDataLayout(Handle));
}

public readonly record struct DeclReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public string? Name => Marshal.PtrToStringUTF8(SlangNative.spReflectionDecl_getName(Handle));

    public SlangDeclKind Kind => SlangNative.spReflectionDecl_getKind(Handle);

    public uint ChildrenCount => SlangNative.spReflectionDecl_getChildrenCount(Handle);

    public DeclReflection GetChild(uint index) =>
        new(SlangNative.spReflectionDecl_getChild(Handle, index));

    public DeclReflection Parent => new(SlangNative.spReflectionDecl_getParent(Handle));

    /// <summary>Cast to <see cref="FunctionReflection"/>. Returns null wrapper if this decl
    /// is not a function. Slang's castTo* functions assume the caller has already verified
    /// the kind, so we Kind-guard here to avoid native-side asserts/crashes.</summary>
    public FunctionReflection AsFunction() => Kind == SlangDeclKind.Func
        ? new(SlangNative.spReflectionDecl_castToFunction(Handle))
        : default;

    /// <summary>Cast to <see cref="VariableReflection"/>. Returns null wrapper if this decl
    /// is not a variable.</summary>
    public VariableReflection AsVariable() => Kind == SlangDeclKind.Variable
        ? new(SlangNative.spReflectionDecl_castToVariable(Handle))
        : default;

    /// <summary>Cast to <see cref="GenericReflection"/>. Returns null wrapper if this decl
    /// is not a generic.</summary>
    public GenericReflection AsGeneric() => Kind == SlangDeclKind.Generic
        ? new(SlangNative.spReflectionDecl_castToGeneric(Handle))
        : default;

    public ModifierReflection FindModifier(SlangModifierID id) =>
        new(SlangNative.spReflectionDecl_findModifier(Handle, id));

    public TypeReflection AsType() => new(SlangNative.spReflection_getTypeFromDecl(Handle));
}

public readonly record struct FunctionReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public string? Name => Marshal.PtrToStringUTF8(SlangNative.spReflectionFunction_GetName(Handle));

    public DeclReflection AsDecl() => new(SlangNative.spReflectionFunction_asDecl(Handle));

    public TypeReflection ResultType => new(SlangNative.spReflectionFunction_GetResultType(Handle));

    public uint ParameterCount => SlangNative.spReflectionFunction_GetParameterCount(Handle);

    public VariableReflection GetParameter(uint index) =>
        new(SlangNative.spReflectionFunction_GetParameter(Handle, index));

    public uint UserAttributeCount => SlangNative.spReflectionFunction_GetUserAttributeCount(Handle);

    public UserAttributeReflection GetUserAttribute(uint index) =>
        new(SlangNative.spReflectionFunction_GetUserAttribute(Handle, index));

    public UserAttributeReflection FindUserAttributeByName(IntPtr globalSession, string name) =>
        new(SlangNative.spReflectionFunction_FindUserAttributeByName(Handle, globalSession, name));

    public ModifierReflection FindModifier(SlangModifierID id) =>
        new(SlangNative.spReflectionFunction_FindModifier(Handle, id));

    public bool HasModifier(SlangModifierID id) => !FindModifier(id).IsNull;

    public GenericReflection GenericContainer =>
        new(SlangNative.spReflectionFunction_GetGenericContainer(Handle));

    public bool IsOverloaded => SlangNative.spReflectionFunction_isOverloaded(Handle) != 0;

    public uint OverloadCount => SlangNative.spReflectionFunction_getOverloadCount(Handle);

    public FunctionReflection GetOverload(uint index) =>
        new(SlangNative.spReflectionFunction_getOverload(Handle, index));
}

public readonly record struct GenericReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public string? Name => Marshal.PtrToStringUTF8(SlangNative.spReflectionGeneric_GetName(Handle));

    public DeclReflection AsDecl() => new(SlangNative.spReflectionGeneric_asDecl(Handle));

    public uint TypeParameterCount =>
        SlangNative.spReflectionGeneric_GetTypeParameterCount(Handle);

    public VariableReflection GetTypeParameter(uint index) =>
        new(SlangNative.spReflectionGeneric_GetTypeParameter(Handle, index));

    public uint ValueParameterCount =>
        SlangNative.spReflectionGeneric_GetValueParameterCount(Handle);

    public VariableReflection GetValueParameter(uint index) =>
        new(SlangNative.spReflectionGeneric_GetValueParameter(Handle, index));

    public uint GetTypeParameterConstraintCount(VariableReflection typeParam) =>
        SlangNative.spReflectionGeneric_GetTypeParameterConstraintCount(Handle, typeParam.Handle);

    public TypeReflection GetTypeParameterConstraintType(VariableReflection typeParam, uint index) =>
        new(SlangNative.spReflectionGeneric_GetTypeParameterConstraintType(Handle, typeParam.Handle, index));

    public SlangDeclKind InnerKind => SlangNative.spReflectionGeneric_GetInnerKind(Handle);

    public DeclReflection InnerDecl =>
        new(SlangNative.spReflectionGeneric_GetInnerDecl(Handle));

    public GenericReflection OuterGenericContainer =>
        new(SlangNative.spReflectionGeneric_GetOuterGenericContainer(Handle));

    public TypeReflection GetConcreteType(VariableReflection typeParam) =>
        new(SlangNative.spReflectionGeneric_GetConcreteType(Handle, typeParam.Handle));

    public long GetConcreteIntVal(VariableReflection valueParam) =>
        SlangNative.spReflectionGeneric_GetConcreteIntVal(Handle, valueParam.Handle);
}

public readonly record struct TypeParameterReflection(IntPtr Handle)
{
    public bool IsNull => Handle == IntPtr.Zero;

    public string? Name =>
        Marshal.PtrToStringUTF8(SlangNative.spReflectionTypeParameter_GetName(Handle));

    public uint Index => SlangNative.spReflectionTypeParameter_GetIndex(Handle);

    public uint ConstraintCount =>
        SlangNative.spReflectionTypeParameter_GetConstraintCount(Handle);

    public TypeReflection GetConstraintByIndex(uint index) =>
        new(SlangNative.spReflectionTypeParameter_GetConstraintByIndex(Handle, index));
}
