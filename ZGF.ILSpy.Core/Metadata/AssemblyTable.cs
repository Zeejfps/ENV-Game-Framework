// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the Assembly table — at most one row, present only when the metadata file is
	/// itself an assembly manifest (vs. a plain .netmodule). Carries the assembly's name,
	/// culture, version, hash algorithm, and signing flags.
	/// </summary>
	internal sealed class AssemblyTable : MetadataTable<AssemblyTable.AssemblyEntry>
	{
		public AssemblyTable(MetadataFile metadataFile)
			: base(TableIndex.Assembly, metadataFile)
		{
		}

		protected override IReadOnlyList<AssemblyEntry> LoadTable()
			=> metadataFile.Metadata.IsAssembly
				? [new AssemblyEntry(metadataFile.Metadata, metadataFile.MetadataOffset)]
				: [];

		public sealed class AssemblyEntry
		{
			readonly int metadataOffset;
			readonly MetadataReader metadata;
			readonly AssemblyDefinition assembly;

			public int RID => MetadataTokens.GetRowNumber(EntityHandle.AssemblyDefinition);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(EntityHandle.AssemblyDefinition);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadata, metadataOffset, TableIndex.Assembly, RID);

			[ColumnInfo("X4")]
			public AssemblyHashAlgorithm HashAlgorithm => assembly.HashAlgorithm;

			public object HashAlgorithmTooltip => new FlagsTooltip() {
				FlagGroup.CreateSingleChoiceGroup(typeof(AssemblyHashAlgorithm), selectedValue: (int)assembly.HashAlgorithm, defaultFlag: new Flag("None (0000)", 0, false), includeAny: false)
			};

			[ColumnInfo("X4")]
			public AssemblyFlags Flags => assembly.Flags;

			public object FlagsTooltip => new FlagsTooltip() {
				FlagGroup.CreateMultipleChoiceGroup(typeof(AssemblyFlags), selectedValue: (int)assembly.Flags, includeAll: false)
			};

			public Version Version => assembly.Version;

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(assembly.Name):X} \"{Name}\"";

			public string Name => metadata.GetString(assembly.Name);

			public string CultureTooltip => $"{MetadataTokens.GetHeapOffset(assembly.Culture):X} \"{Culture}\"";

			public string Culture => metadata.GetString(assembly.Culture);

			public AssemblyEntry(MetadataReader metadata, int metadataOffset)
			{
				this.metadata = metadata;
				this.metadataOffset = metadataOffset;
				assembly = metadata.GetAssemblyDefinition();
			}
		}
	}
}
