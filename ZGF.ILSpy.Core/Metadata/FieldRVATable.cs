// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the FieldRVA table — for static fields backed by a fixed PE-image RVA, the
	/// table maps each FieldDef to its data location. Common with <c>fixed</c> array initializers
	/// (the compiler emits a synthesised type whose static fields point at the .rdata bytes).
	/// </summary>
	internal sealed class FieldRVATable : MetadataTable<FieldRVATable.FieldRVAEntry>
	{
		public FieldRVATable(MetadataFile metadataFile)
			: base(TableIndex.FieldRva, metadataFile)
		{
		}

		protected override IReadOnlyList<FieldRVAEntry> LoadTable()
		{
			var list = new List<FieldRVAEntry>();
			foreach (var (rva, field) in metadataFile.Metadata.GetFieldRvas())
				list.Add(new FieldRVAEntry(metadataFile, list.Count + 1, rva, field));
			return list;
		}

		public sealed class FieldRVAEntry
		{
			// Verbatim fieldHandle — see FieldLayoutTable for the C# 14 keyword note.
			readonly MetadataFile metadataFile;
			readonly FieldDefinitionHandle fieldHandle;

			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x1D000000 | RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Field => MetadataTokens.GetToken(fieldHandle);

			string? fieldTooltip;
			public string? FieldTooltip => GenerateTooltip(ref fieldTooltip, metadataFile, fieldHandle);

			[ColumnInfo("X8")]
			public int RVA { get; }

			public FieldRVAEntry(MetadataFile metadataFile, int rid, int rva, FieldDefinitionHandle field)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				RVA = rva;
				fieldHandle = field;
			}
		}
	}
}
