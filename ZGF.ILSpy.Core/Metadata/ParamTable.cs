// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the Param table — every formal parameter declared on every method body. Walked
	/// by 1-based row index because the metadata reader doesn't expose Param as an IEnumerable.
	/// </summary>
	internal sealed class ParamTable : MetadataTable<ParamTable.ParamEntry>
	{
		public ParamTable(MetadataFile metadataFile)
			: base(TableIndex.Param, metadataFile)
		{
		}

		protected override IReadOnlyList<ParamEntry> LoadTable()
		{
			var list = new List<ParamEntry>();
			for (int row = 1; row <= metadataFile.Metadata.GetTableRowCount(TableIndex.Param); row++)
				list.Add(new ParamEntry(metadataFile, MetadataTokens.ParameterHandle(row)));
			return list;
		}

		public sealed class ParamEntry
		{
			readonly MetadataFile metadataFile;
			readonly ParameterHandle handle;
			readonly Parameter param;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.Param, RID);

			[ColumnInfo("X8")]
			public ParameterAttributes Attributes => param.Attributes;

			public object AttributesTooltip => new FlagsTooltip {
				FlagGroup.CreateMultipleChoiceGroup(typeof(ParameterAttributes), selectedValue: (int)param.Attributes, includeAll: false)
			};

			public int Sequence => param.SequenceNumber;

			public string Name => metadataFile.Metadata.GetString(param.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(param.Name):X} \"{Name}\"";

			public ParamEntry(MetadataFile metadataFile, ParameterHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				param = metadataFile.Metadata.GetParameter(handle);
			}
		}
	}
}
