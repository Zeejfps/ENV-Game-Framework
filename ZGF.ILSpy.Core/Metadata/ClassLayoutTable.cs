// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the ClassLayout table — explicit field-layout overrides on TypeDefs marked
	/// Sequential or Explicit (e.g. <c>[StructLayout(LayoutKind.Explicit)]</c>). System.Reflection.Metadata
	/// doesn't surface these rows through a typed enumeration so we walk the table bytes
	/// directly via a BlobReader.
	/// </summary>
	internal sealed class ClassLayoutTable : MetadataTable<ClassLayoutTable.ClassLayoutEntry>
	{
		public ClassLayoutTable(MetadataFile metadataFile)
			: base(TableIndex.ClassLayout, metadataFile)
		{
		}

		protected override IReadOnlyList<ClassLayoutEntry> LoadTable()
		{
			var list = new List<ClassLayoutEntry>();
			var metadata = metadataFile.Metadata;
			var length = metadata.GetTableRowCount(TableIndex.ClassLayout);
			var reader = metadata.AsBlobReader();
			reader.Offset = metadata.GetTableMetadataOffset(TableIndex.ClassLayout);
			int typeDefSize = metadata.GetTableRowCount(TableIndex.TypeDef) < ushort.MaxValue ? 2 : 4;
			for (int rid = 1; rid <= length; rid++)
			{
				ushort packingSize = reader.ReadUInt16();
				uint classSize = reader.ReadUInt32();
				int parentRow = typeDefSize == 2 ? reader.ReadUInt16() : reader.ReadInt32();
				list.Add(new ClassLayoutEntry(metadataFile, rid, packingSize, classSize, MetadataTokens.TypeDefinitionHandle(parentRow)));
			}
			return list;
		}

		public sealed class ClassLayoutEntry
		{
			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x0F000000 | RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Parent => MetadataTokens.GetToken(parent);

			string? parentTooltip;
			public string? ParentTooltip => GenerateTooltip(ref parentTooltip, metadataFile, parent);

			public ushort PackingSize { get; }

			public uint ClassSize { get; }

			readonly MetadataFile metadataFile;
			readonly TypeDefinitionHandle parent;

			public ClassLayoutEntry(MetadataFile metadataFile, int rid, ushort packingSize, uint classSize, TypeDefinitionHandle parent)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				PackingSize = packingSize;
				ClassSize = classSize;
				this.parent = parent;
			}
		}
	}
}
