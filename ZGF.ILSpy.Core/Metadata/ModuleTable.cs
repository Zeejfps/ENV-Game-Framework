// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the Module table — always exactly one row carrying the module name and the
	/// MVID (the GUID identifying this module's identity). Reads the GUID heap for MVID and
	/// the optional generation IDs (only present in EnC / hot-reload deltas).
	/// </summary>
	internal sealed class ModuleTable : MetadataTable<ModuleTable.ModuleEntry>
	{
		public ModuleTable(MetadataFile metadataFile)
			: base(TableIndex.Module, metadataFile)
		{
		}

		protected override IReadOnlyList<ModuleEntry> LoadTable()
			=> [new ModuleEntry(metadataFile, (ModuleDefinitionHandle)EntityHandle.ModuleDefinition)];

		public sealed class ModuleEntry
		{
			readonly MetadataFile metadataFile;
			readonly ModuleDefinitionHandle handle;
			readonly ModuleDefinition moduleDef;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.Module, RID);

			public int Generation => moduleDef.Generation;

			public string Name => metadataFile.Metadata.GetString(moduleDef.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(moduleDef.Name):X} \"{Name}\"";

			[ColumnInfo("X8")]
			public int Mvid => MetadataTokens.GetHeapOffset(moduleDef.Mvid);

			public string MvidTooltip => metadataFile.Metadata.GetGuid(moduleDef.Mvid).ToString();

			[ColumnInfo("X8")]
			public int GenerationId => MetadataTokens.GetHeapOffset(moduleDef.GenerationId);

			public string? GenerationIdTooltip => moduleDef.GenerationId.IsNil ? null : metadataFile.Metadata.GetGuid(moduleDef.GenerationId).ToString();

			[ColumnInfo("X8")]
			public int BaseGenerationId => MetadataTokens.GetHeapOffset(moduleDef.BaseGenerationId);

			public string? BaseGenerationIdTooltip => moduleDef.BaseGenerationId.IsNil ? null : metadataFile.Metadata.GetGuid(moduleDef.BaseGenerationId).ToString();

			public ModuleEntry(MetadataFile metadataFile, ModuleDefinitionHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				moduleDef = metadataFile.Metadata.GetModuleDefinition();
			}
		}
	}
}
