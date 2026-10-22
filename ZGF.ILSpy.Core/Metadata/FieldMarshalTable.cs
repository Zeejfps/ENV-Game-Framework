// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the FieldMarshal table — native-marshalling descriptors attached to fields
	/// or parameters. Parent is a HasFieldMarshal coded token (Field / Param); NativeType
	/// is the heap-offset of a marshal-spec blob.
	/// </summary>
	internal sealed class FieldMarshalTable : MetadataTable<FieldMarshalTable.FieldMarshalEntry>
	{
		public FieldMarshalTable(MetadataFile metadataFile)
			: base(TableIndex.FieldMarshal, metadataFile)
		{
		}

		protected override IReadOnlyList<FieldMarshalEntry> LoadTable()
		{
			var list = new List<FieldMarshalEntry>();
			int rid = 0;
			foreach (var (parent, nativeType) in metadataFile.Metadata.GetFieldMarshals())
			{
				list.Add(new FieldMarshalEntry(metadataFile, ++rid, parent, nativeType));
			}
			return list;
		}

		public sealed class FieldMarshalEntry
		{
			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x0D000000 | RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Parent => MetadataTokens.GetToken(parent);

			string? parentTooltip;
			public string? ParentTooltip => GenerateTooltip(ref parentTooltip, metadataFile, parent);

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int NativeType => MetadataTokens.GetHeapOffset(nativeType);

			readonly MetadataFile metadataFile;
			readonly EntityHandle parent;
			readonly BlobHandle nativeType;

			public FieldMarshalEntry(MetadataFile metadataFile, int rid, EntityHandle parent, BlobHandle nativeType)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				this.parent = parent;
				this.nativeType = nativeType;
			}
		}
	}
}
