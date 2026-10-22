// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the ManifestResource table — every embedded or linked resource the assembly
	/// manifest publishes. Implementation is an Implementation coded token: nil for embedded
	/// resources (data lives at .text+offset), otherwise an AssemblyFile or AssemblyRef
	/// pointing at the holder of the resource bytes.
	/// </summary>
	internal sealed class ManifestResourceTable : MetadataTable<ManifestResourceTable.ManifestResourceEntry>
	{
		public ManifestResourceTable(MetadataFile metadataFile)
			: base(TableIndex.ManifestResource, metadataFile)
		{
		}

		protected override IReadOnlyList<ManifestResourceEntry> LoadTable()
		{
			var list = new List<ManifestResourceEntry>();
			foreach (var row in metadataFile.Metadata.ManifestResources)
				list.Add(new ManifestResourceEntry(metadataFile, row));
			return list;
		}

		public sealed class ManifestResourceEntry
		{
			readonly MetadataFile metadataFile;
			readonly ManifestResourceHandle handle;
			readonly ManifestResource manifestResource;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.ManifestResource, RID);

			[ColumnInfo("X8")]
			public ManifestResourceAttributes Attributes => manifestResource.Attributes;

			public object AttributesTooltip => manifestResource.Attributes.ToString();

			public string Name => metadataFile.Metadata.GetString(manifestResource.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(manifestResource.Name):X} \"{Name}\"";

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Implementation => MetadataTokens.GetToken(manifestResource.Implementation);

			string? implementationTooltip;
			public string? ImplementationTooltip => GenerateTooltip(ref implementationTooltip, metadataFile, manifestResource.Implementation);

			public ManifestResourceEntry(MetadataFile metadataFile, ManifestResourceHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				manifestResource = metadataFile.Metadata.GetManifestResource(handle);
			}
		}
	}
}
