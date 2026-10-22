// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the MemberRef table — every external method or field the module references.
	/// Parent is a MemberRefParent coded token (TypeRef / ModuleRef / MethodDef / TypeSpec /
	/// TypeDef) carrying the owner of the referenced member.
	/// </summary>
	internal sealed class MemberRefTable : MetadataTable<MemberRefTable.MemberRefEntry>
	{
		public MemberRefTable(MetadataFile metadataFile)
			: base(TableIndex.MemberRef, metadataFile)
		{
		}

		protected override IReadOnlyList<MemberRefEntry> LoadTable()
		{
			var list = new List<MemberRefEntry>();
			foreach (var row in metadataFile.Metadata.MemberReferences)
				list.Add(new MemberRefEntry(metadataFile, row));
			return list;
		}

		public sealed class MemberRefEntry
		{
			readonly MetadataFile metadataFile;
			readonly MemberReferenceHandle handle;
			readonly MemberReference memberRef;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.MemberRef, RID);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Parent => MetadataTokens.GetToken(memberRef.Parent);

			string? parentTooltip;
			public string? ParentTooltip => GenerateTooltip(ref parentTooltip, metadataFile, memberRef.Parent);

			public string Name => metadataFile.Metadata.GetString(memberRef.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(memberRef.Name):X} \"{Name}\"";

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Signature => MetadataTokens.GetHeapOffset(memberRef.Signature);

			string? signatureTooltip;
			public string? SignatureTooltip => GenerateTooltip(ref signatureTooltip, metadataFile, handle);

			public MemberRefEntry(MetadataFile metadataFile, MemberReferenceHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				memberRef = metadataFile.Metadata.GetMemberReference(handle);
			}
		}
	}
}
