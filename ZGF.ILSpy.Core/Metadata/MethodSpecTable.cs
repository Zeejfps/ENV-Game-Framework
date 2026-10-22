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
	/// View of the MethodSpec table — concrete instantiations of generic methods like
	/// <c>List&lt;int&gt;.Add</c>. Each row points back at the open generic method
	/// (MethodDef or MemberRef) and carries a blob holding the type-argument list.
	/// </summary>
	internal sealed class MethodSpecTable : MetadataTable<MethodSpecTable.MethodSpecEntry>
	{
		public MethodSpecTable(MetadataFile metadataFile)
			: base(TableIndex.MethodSpec, metadataFile)
		{
		}

		protected override IReadOnlyList<MethodSpecEntry> LoadTable()
		{
			var list = new List<MethodSpecEntry>();
			foreach (var row in metadataFile.Metadata.GetMethodSpecifications())
				list.Add(new MethodSpecEntry(metadataFile, row));
			return list;
		}

		public sealed class MethodSpecEntry
		{
			readonly MetadataFile metadataFile;
			readonly MethodSpecificationHandle handle;
			readonly MethodSpecification methodSpec;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.MethodSpec, RID);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Method => MetadataTokens.GetToken(methodSpec.Method);

			string? methodTooltip;
			public string? MethodTooltip => GenerateTooltip(ref methodTooltip, metadataFile, methodSpec.Method);

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Signature => MetadataTokens.GetHeapOffset(methodSpec.Signature);

			public string? SignatureTooltip {
				get {
					ITextOutput output = new PlainTextOutput();
					var signature = methodSpec.DecodeSignature(new DisassemblerSignatureTypeProvider(metadataFile, output), default);
					bool first = true;
					foreach (var type in signature)
					{
						if (first)
							first = false;
						else
							output.Write(", ");
						type(ILNameSyntax.TypeName);
					}
					return output.ToString();
				}
			}

			public MethodSpecEntry(MetadataFile metadataFile, MethodSpecificationHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				methodSpec = metadataFile.Metadata.GetMethodSpecification(handle);
			}
		}
	}
}
