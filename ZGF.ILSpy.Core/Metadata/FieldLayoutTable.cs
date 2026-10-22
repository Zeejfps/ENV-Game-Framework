// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the FieldLayout table — explicit per-field byte offsets for fields whose
	/// owning TypeDef has <see cref="System.Reflection.TypeAttributes.ExplicitLayout"/> set.
	/// </summary>
	internal sealed class FieldLayoutTable : MetadataTable<FieldLayoutTable.FieldLayoutEntry>
	{
		public FieldLayoutTable(MetadataFile metadataFile)
			: base(TableIndex.FieldLayout, metadataFile)
		{
		}

		protected override IReadOnlyList<FieldLayoutEntry> LoadTable()
		{
			var list = new List<FieldLayoutEntry>();
			foreach (var (offset, field) in metadataFile.Metadata.GetFieldLayoutRows())
				list.Add(new FieldLayoutEntry(metadataFile, list.Count + 1, offset, field));
			return list;
		}

		public sealed class FieldLayoutEntry
		{
			// Use a verbatim fieldHandle local because C# 14 made `field` contextual inside property
			// accessors — referencing it without `@` would name the auto-property's hidden
			// backing field instead of this member.
			readonly MetadataFile metadataFile;
			readonly FieldDefinitionHandle fieldHandle;

			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x10000000 | RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Field => MetadataTokens.GetToken(fieldHandle);

			string? fieldTooltip;
			public string? FieldTooltip => GenerateTooltip(ref fieldTooltip, metadataFile, fieldHandle);

			[ColumnInfo("X8")]
			public int FieldOffset { get; }

			public FieldLayoutEntry(MetadataFile metadataFile, int rid, int fieldOffset, FieldDefinitionHandle field)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				FieldOffset = fieldOffset;
				fieldHandle = field;
			}
		}
	}
}
