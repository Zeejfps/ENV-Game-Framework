// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the TypeSpec table — instantiated generic types and other constructed type
	/// shapes (arrays, pointers, modreq/modopt) referenced from instructions and signatures.
	/// Each row points at a blob signature describing the constructed type.
	/// </summary>
	internal sealed class TypeSpecTable : MetadataTable<TypeSpecTable.TypeSpecEntry>
	{
		public TypeSpecTable(MetadataFile metadataFile)
			: base(TableIndex.TypeSpec, metadataFile)
		{
		}

		protected override IReadOnlyList<TypeSpecEntry> LoadTable()
		{
			var list = new List<TypeSpecEntry>();
			foreach (var row in metadataFile.Metadata.GetTypeSpecifications())
				list.Add(new TypeSpecEntry(metadataFile, row));
			return list;
		}

		public sealed class TypeSpecEntry
		{
			readonly MetadataFile metadataFile;
			readonly TypeSpecificationHandle handle;
			readonly TypeSpecification typeSpec;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.TypeSpec, RID);

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Signature => MetadataTokens.GetHeapOffset(typeSpec.Signature);

			public string? SignatureTooltip {
				get {
					ITextOutput output = new PlainTextOutput();
					typeSpec.DecodeSignature(new DisassemblerSignatureTypeProvider(metadataFile, output), default)(ILNameSyntax.Signature);
					return output.ToString();
				}
			}

			public TypeSpecEntry(MetadataFile metadataFile, TypeSpecificationHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				typeSpec = metadataFile.Metadata.GetTypeSpecification(handle);
			}
		}
	}
}
