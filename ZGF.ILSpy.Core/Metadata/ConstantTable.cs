// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the Constant table — compile-time constants attached to fields, parameters,
	/// or properties. The Type column is the primitive type code; Value points at a blob
	/// holding the literal payload.
	/// </summary>
	internal sealed class ConstantTable : MetadataTable<ConstantTable.ConstantEntry>
	{
		public ConstantTable(MetadataFile metadataFile)
			: base(TableIndex.Constant, metadataFile)
		{
		}

		protected override IReadOnlyList<ConstantEntry> LoadTable()
		{
			var list = new List<ConstantEntry>();
			for (int row = 1; row <= metadataFile.Metadata.GetTableRowCount(TableIndex.Constant); row++)
				list.Add(new ConstantEntry(metadataFile, MetadataTokens.ConstantHandle(row)));
			return list;
		}

		public sealed class ConstantEntry
		{
			readonly MetadataFile metadataFile;
			readonly ConstantHandle handle;
			readonly Constant constant;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.Constant, RID);

			[ColumnInfo("X8")]
			public ConstantTypeCode Type => constant.TypeCode;

			public string TypeTooltip => constant.TypeCode.ToString();

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Parent => MetadataTokens.GetToken(constant.Parent);

			string? parentTooltip;
			public string? ParentTooltip => GenerateTooltip(ref parentTooltip, metadataFile, constant.Parent);

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Value => MetadataTokens.GetHeapOffset(constant.Value);

			public string? ValueTooltip {
				get {
					return null;
				}
			}

			public ConstantEntry(MetadataFile metadataFile, ConstantHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				constant = metadataFile.Metadata.GetConstant(handle);
			}
		}
	}
}
