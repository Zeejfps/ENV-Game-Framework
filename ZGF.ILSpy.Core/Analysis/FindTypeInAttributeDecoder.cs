// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>What decoding an attribute type found: <see cref="Found"/> when it is the type searched
/// for, plus while decoding an enum its underlying <see cref="PrimitiveTypeCode"/>, or
/// <see cref="SystemType"/> for <see cref="System.Type"/>.</summary>
internal enum TokenSearchResult : byte
{
    NoResult = 0,
    TypeCodeMask = 0b0111_1111,
    Found = 0b1000_0000,
    SystemType = 127,
}

/// <summary>Finds a type in the attribute values of a module.</summary>
internal sealed class FindTypeInAttributeDecoder : ICustomAttributeTypeProvider<TokenSearchResult>
{
    private readonly MetadataFile _declaringModule;
    private readonly MetadataModule _currentModule;
    private readonly TypeDefinitionHandle _handle;
    private readonly PrimitiveTypeCode _primitiveType;

    public FindTypeInAttributeDecoder(MetadataModule currentModule, ITypeDefinition type)
    {
        _currentModule = currentModule;
        _declaringModule = type.ParentModule?.MetadataFile ?? throw new InvalidOperationException("Cannot use MetadataModule without PEFile as context.");
        _handle = (TypeDefinitionHandle)type.MetadataToken;
        _primitiveType = type.KnownTypeCode == KnownTypeCode.None ? 0 : type.KnownTypeCode.ToPrimitiveTypeCode();
    }

    public TokenSearchResult GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode == _primitiveType ? TokenSearchResult.Found : 0;

    public TokenSearchResult GetSystemType() => TokenSearchResult.SystemType;

    public TokenSearchResult GetSZArrayType(TokenSearchResult elementType) => elementType & TokenSearchResult.Found;

    public TokenSearchResult GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var result = TokenSearchResult.NoResult;
        if (handle.IsEnum(reader, out var underlyingType))
            result = (TokenSearchResult)underlyingType;
        else if (((EntityHandle)handle).IsKnownType(reader, KnownTypeCode.Type))
            result = TokenSearchResult.SystemType;
        if (_handle == handle && reader == _declaringModule.Metadata)
            result |= TokenSearchResult.Found;
        return result;
    }

    public TokenSearchResult GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        GetResultFromResolvedType(_currentModule.ResolveType(handle, default));

    public TokenSearchResult GetTypeFromSerializedName(string name)
    {
        if (name == null)
            return TokenSearchResult.NoResult;
        try
        {
            return GetResultFromResolvedType(ReflectionHelper.ParseReflectionName(name, new SimpleTypeResolveContext(_currentModule)));
        }
        catch (ReflectionNameParseException)
        {
            return TokenSearchResult.NoResult;
        }
    }

    private TokenSearchResult GetResultFromResolvedType(IType type)
    {
        var definition = type.GetDefinition();
        if (definition == null)
            return TokenSearchResult.NoResult;
        var result = TokenSearchResult.NoResult;
        var underlyingType = definition.EnumUnderlyingType?.GetDefinition();
        if (underlyingType != null)
            result = (TokenSearchResult)underlyingType.KnownTypeCode.ToPrimitiveTypeCode();
        else if (definition.KnownTypeCode == KnownTypeCode.Type)
            result = TokenSearchResult.SystemType;
        if (definition.MetadataToken == _handle && definition.ParentModule?.MetadataFile == _declaringModule)
            result |= TokenSearchResult.Found;
        return result;
    }

    public PrimitiveTypeCode GetUnderlyingEnumType(TokenSearchResult type)
    {
        var typeCode = type & TokenSearchResult.TypeCodeMask;
        if (typeCode == 0 || typeCode == TokenSearchResult.SystemType)
            throw new EnumUnderlyingTypeResolveException();
        return (PrimitiveTypeCode)typeCode;
    }

    public bool IsSystemType(TokenSearchResult type) => (type & TokenSearchResult.TypeCodeMask) == TokenSearchResult.SystemType;
}
