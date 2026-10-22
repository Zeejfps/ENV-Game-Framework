// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the LocalVariable table — debug-info for named local slots inside method
	/// bodies. Each row carries the variable's name, slot index, and PDB-only attributes
	/// (e.g. "is compiler-generated").
	/// </summary>
	internal sealed class LocalVariableTable : MetadataTable<LocalVariableTable.LocalVariableEntry>
	{
		public LocalVariableTable(MetadataFile metadataFile)
			: base(TableIndex.LocalVariable, metadataFile)
		{
		}

		protected override IReadOnlyList<LocalVariableEntry> LoadTable()
		{
			var list = new List<LocalVariableEntry>();
			foreach (var row in metadataFile.Metadata.LocalVariables)
				list.Add(new LocalVariableEntry(metadataFile, row));
			return list;
		}

		public sealed class LocalVariableEntry
		{
			readonly MetadataFile metadataFile;
			readonly LocalVariableHandle handle;
			readonly LocalVariable localVar;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public LocalVariableAttributes Attributes => localVar.Attributes;

			public object AttributesTooltip => new FlagsTooltip() {
				FlagGroup.CreateMultipleChoiceGroup(typeof(LocalVariableAttributes)),
			};

			public int Index => localVar.Index;

			public string Name => metadataFile.Metadata.GetString(localVar.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(localVar.Name):X} \"{Name}\"";

			public LocalVariableEntry(MetadataFile metadataFile, LocalVariableHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				localVar = metadataFile.Metadata.GetLocalVariable(handle);
			}
		}
	}
}
