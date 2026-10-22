// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the ModuleRef table — every external .netmodule the assembly P/Invokes into
	/// or otherwise references by name. Carries just the module's name string handle.
	/// </summary>
	internal sealed class ModuleRefTable : MetadataTable<ModuleRefTable.ModuleRefEntry>
	{
		public ModuleRefTable(MetadataFile metadataFile)
			: base(TableIndex.ModuleRef, metadataFile)
		{
		}

		protected override IReadOnlyList<ModuleRefEntry> LoadTable()
		{
			var list = new List<ModuleRefEntry>();
			foreach (var row in metadataFile.Metadata.GetModuleReferences())
				list.Add(new ModuleRefEntry(metadataFile, row));
			return list;
		}

		public sealed class ModuleRefEntry
		{
			readonly MetadataFile metadataFile;
			readonly ModuleReferenceHandle handle;
			readonly ModuleReference moduleRef;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.ModuleRef, RID);

			public string Name => metadataFile.Metadata.GetString(moduleRef.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(moduleRef.Name):X} \"{Name}\"";

			public ModuleRefEntry(MetadataFile metadataFile, ModuleReferenceHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				moduleRef = metadataFile.Metadata.GetModuleReference(handle);
			}
		}
	}
}
