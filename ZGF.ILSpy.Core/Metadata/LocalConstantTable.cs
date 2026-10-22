// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the LocalConstant table — named compile-time constants visible inside a
	/// method body's debug info. Each row carries the constant's name and a blob holding
	/// its type signature + serialised value.
	/// </summary>
	internal sealed class LocalConstantTable : MetadataTable<LocalConstantTable.LocalConstantEntry>
	{
		public LocalConstantTable(MetadataFile metadataFile)
			: base(TableIndex.LocalConstant, metadataFile)
		{
		}

		protected override IReadOnlyList<LocalConstantEntry> LoadTable()
		{
			var list = new List<LocalConstantEntry>();
			foreach (var row in metadataFile.Metadata.LocalConstants)
				list.Add(new LocalConstantEntry(metadataFile, row));
			return list;
		}

		public sealed class LocalConstantEntry
		{
			readonly MetadataFile metadataFile;
			readonly LocalConstantHandle handle;
			readonly LocalConstant localConst;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			public string Name => metadataFile.Metadata.GetString(localConst.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(localConst.Name):X} \"{Name}\"";

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Signature => MetadataTokens.GetHeapOffset(localConst.Signature);

			public LocalConstantEntry(MetadataFile metadataFile, LocalConstantHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				localConst = metadataFile.Metadata.GetLocalConstant(handle);
			}
		}
	}
}
