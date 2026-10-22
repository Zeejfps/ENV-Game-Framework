// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

#nullable enable

using System;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	public enum TokenSearchResult : byte
	{
		NoResult = 0,
		Byte = PrimitiveTypeCode.Byte,
		SByte = PrimitiveTypeCode.SByte,
		Int16 = PrimitiveTypeCode.Int16,
		UInt16 = PrimitiveTypeCode.UInt16,
		Int32 = PrimitiveTypeCode.Int32,
		UInt32 = PrimitiveTypeCode.UInt32,
		Int64 = PrimitiveTypeCode.Int64,
		UInt64 = PrimitiveTypeCode.UInt64,
		IntPtr = PrimitiveTypeCode.IntPtr,
		UIntPtr = PrimitiveTypeCode.UIntPtr,

		// lowest PrimitiveTypeCode is 1
		// highest PrimitiveTypeCode is 28 (0b0001_1100)
		// TokenSearchResult with a PrimitiveTypeCode set is only used when decoding an enum-type.
		// It is used for GetUnderlyingEnumType and should be masked out in all other uses.
		// MSB = Found
		// 127 = System.Type
		TypeCodeMask = 0b0111_1111,
		Found = 0b1000_0000,
		SystemType = 127,
	}

	class FindTypeInAttributeDecoder : ICustomAttributeTypeProvider<TokenSearchResult>
	{
		readonly MetadataFile declaringModule;
		readonly MetadataModule currentModule;
		readonly TypeDefinitionHandle handle;
		readonly PrimitiveTypeCode primitiveType;

		/// <summary>
		/// Constructs a FindTypeInAttributeDecoder that can be used to find <paramref name="type"/> in signatures from <paramref name="currentModule"/>.
		/// </summary>
		public FindTypeInAttributeDecoder(MetadataModule currentModule, ITypeDefinition type)
		{
			this.currentModule = currentModule;
			this.declaringModule = type.ParentModule?.MetadataFile ?? throw new InvalidOperationException("Cannot use MetadataModule without PEFile as context.");
			this.handle = (TypeDefinitionHandle)type.MetadataToken;
			this.primitiveType = type.KnownTypeCode == KnownTypeCode.None ? 0 : type.KnownTypeCode.ToPrimitiveTypeCode();
		}

		public TokenSearchResult GetPrimitiveType(PrimitiveTypeCode typeCode)
		{
			return typeCode == primitiveType ? TokenSearchResult.Found : 0;
		}

		public TokenSearchResult GetSystemType() => TokenSearchResult.SystemType;

		public TokenSearchResult GetSZArrayType(TokenSearchResult elementType) => elementType & TokenSearchResult.Found;

		public TokenSearchResult GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
		{
			TokenSearchResult result = TokenSearchResult.NoResult;

			if (handle.IsEnum(reader, out PrimitiveTypeCode underlyingType))
			{
				result = (TokenSearchResult)underlyingType;
			}
			else if (((EntityHandle)handle).IsKnownType(reader, KnownTypeCode.Type))
			{
				result = TokenSearchResult.SystemType;
			}
			if (this.handle == handle && reader == declaringModule.Metadata)
			{
				result |= TokenSearchResult.Found;
			}
			return result;
		}

		public TokenSearchResult GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
		{
			var t = currentModule.ResolveType(handle, default);
			return GetResultFromResolvedType(t);
		}

		public TokenSearchResult GetTypeFromSerializedName(string name)
		{
			if (name == null)
			{
				return TokenSearchResult.NoResult;
			}
			try
			{
				IType type = ReflectionHelper.ParseReflectionName(name, new SimpleTypeResolveContext(currentModule));
				return GetResultFromResolvedType(type);
			}
			catch (ReflectionNameParseException)
			{
				return TokenSearchResult.NoResult;
			}
		}

		private TokenSearchResult GetResultFromResolvedType(IType type)
		{
			var td = type.GetDefinition();
			if (td == null)
				return TokenSearchResult.NoResult;

			TokenSearchResult result = TokenSearchResult.NoResult;
			var underlyingType = td.EnumUnderlyingType?.GetDefinition();
			if (underlyingType != null)
			{
				result = (TokenSearchResult)underlyingType.KnownTypeCode.ToPrimitiveTypeCode();
			}
			else if (td.KnownTypeCode == KnownTypeCode.Type)
			{
				result = TokenSearchResult.SystemType;
			}
			if (td.MetadataToken == this.handle && td.ParentModule?.MetadataFile == declaringModule)
			{
				result |= TokenSearchResult.Found;
			}
			return result;
		}

		public PrimitiveTypeCode GetUnderlyingEnumType(TokenSearchResult type)
		{
			TokenSearchResult typeCode = type & TokenSearchResult.TypeCodeMask;
			if (typeCode == 0 || typeCode == TokenSearchResult.SystemType)
				throw new EnumUnderlyingTypeResolveException();
			return (PrimitiveTypeCode)typeCode;
		}

		public bool IsSystemType(TokenSearchResult type) => (type & TokenSearchResult.TypeCodeMask) == TokenSearchResult.SystemType;
	}
}
