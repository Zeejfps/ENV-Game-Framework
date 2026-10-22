// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the five Ptr tables (FieldPtr / MethodPtr / ParamPtr / EventPtr / PropertyPtr).
	/// Present only in metadata produced with #~ + indirection (rare; mostly EnC or some
	/// custom emitters), and each row carries a single handle into the table the Ptr points
	/// at. One node type drives all five since the row shape is identical.
	/// </summary>
	internal sealed class PtrTable : MetadataTable<PtrTable.PtrEntry>
	{
		readonly TableIndex referencedTableKind;

		public PtrTable(TableIndex kind, MetadataFile metadataFile)
			: base(kind, metadataFile)
		{
			referencedTableKind = kind switch {
				TableIndex.EventPtr => TableIndex.Event,
				TableIndex.FieldPtr => TableIndex.Field,
				TableIndex.MethodPtr => TableIndex.MethodDef,
				TableIndex.ParamPtr => TableIndex.Param,
				TableIndex.PropertyPtr => TableIndex.Property,
				_ => throw new ArgumentOutOfRangeException(nameof(kind), $"PtrTable does not support {kind}"),
			};
		}

		protected override IReadOnlyList<PtrEntry> LoadTable()
		{
			var list = new List<PtrEntry>();
			var metadata = metadataFile.Metadata;
			var length = metadata.GetTableRowCount(Kind);
			var reader = metadata.AsBlobReader();
			reader.Offset = metadata.GetTableMetadataOffset(Kind);
			int handleSize = metadata.GetTableRowCount(referencedTableKind) < ushort.MaxValue ? 2 : 4;
			for (int rid = 1; rid <= length; rid++)
			{
				int handleRow = handleSize == 2 ? reader.ReadUInt16() : reader.ReadInt32();
				list.Add(new PtrEntry(metadataFile, rid, Kind, MetadataTokens.EntityHandle(((int)referencedTableKind << 24) | handleRow)));
			}
			return list;
		}

		public sealed class PtrEntry
		{
			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => ((int)kind << 24) | RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Handle => MetadataTokens.GetToken(handle);

			string? handleTooltip;
			public string? HandleTooltip => GenerateTooltip(ref handleTooltip, metadataFile, handle);

			readonly MetadataFile metadataFile;
			readonly TableIndex kind;
			readonly EntityHandle handle;

			public PtrEntry(MetadataFile metadataFile, int rid, TableIndex kind, EntityHandle handle)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				this.kind = kind;
				this.handle = handle;
			}
		}
	}
}
