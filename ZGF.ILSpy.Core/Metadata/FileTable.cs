// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the File table — multi-module assemblies list their constituent .netmodule
	/// files here. Vanishingly rare in modern .NET; nearly every assembly is single-file.
	/// </summary>
	internal sealed class FileTable : MetadataTable<FileTable.FileEntry>
	{
		public FileTable(MetadataFile metadataFile)
			: base(TableIndex.File, metadataFile)
		{
		}

		protected override IReadOnlyList<FileEntry> LoadTable()
		{
			var list = new List<FileEntry>();
			foreach (var row in metadataFile.Metadata.AssemblyFiles)
				list.Add(new FileEntry(metadataFile, row));
			return list;
		}

		public sealed class FileEntry
		{
			readonly MetadataFile metadataFile;
			readonly AssemblyFileHandle handle;
			readonly AssemblyFile assemblyFile;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.File, RID);

			[ColumnInfo("X8")]
			public int Attributes => assemblyFile.ContainsMetadata ? 1 : 0;

			public string AttributesTooltip => assemblyFile.ContainsMetadata ? "ContainsMetaData" : "ContainsNoMetaData";

			public string Name => metadataFile.Metadata.GetString(assemblyFile.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(assemblyFile.Name):X} \"{Name}\"";

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int HashValue => MetadataTokens.GetHeapOffset(assemblyFile.HashValue);

			public string? HashValueTooltip {
				get {
					if (assemblyFile.HashValue.IsNil)
						return null;
					System.Collections.Immutable.ImmutableArray<byte> token = metadataFile.Metadata.GetBlobContent(assemblyFile.HashValue);
					return token.ToHexString(token.Length);
				}
			}

			public FileEntry(MetadataFile metadataFile, AssemblyFileHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				assemblyFile = metadataFile.Metadata.GetAssemblyFile(handle);
			}
		}
	}
}
