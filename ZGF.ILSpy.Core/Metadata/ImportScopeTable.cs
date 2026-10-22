// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the ImportScope table — chains of <c>using</c> / <c>using static</c> / alias
	/// imports active inside a LocalScope. Each row points at its parent ImportScope (or nil
	/// for a root scope) and holds an Imports blob describing the imports in that scope.
	/// </summary>
	internal sealed class ImportScopeTable : MetadataTable<ImportScopeTable.ImportScopeEntry>
	{
		public ImportScopeTable(MetadataFile metadataFile)
			: base(TableIndex.ImportScope, metadataFile)
		{
		}

		protected override IReadOnlyList<ImportScopeEntry> LoadTable()
		{
			var list = new List<ImportScopeEntry>();
			foreach (var row in metadataFile.Metadata.ImportScopes)
				list.Add(new ImportScopeEntry(metadataFile, row));
			return list;
		}

		public sealed class ImportScopeEntry
		{
			readonly MetadataFile metadataFile;
			readonly ImportScopeHandle handle;
			readonly ImportScope scope;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Parent => MetadataTokens.GetToken(scope.Parent);

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Imports => MetadataTokens.GetHeapOffset(scope.ImportsBlob);

			public ImportScopeEntry(MetadataFile metadataFile, ImportScopeHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				scope = metadataFile.Metadata.GetImportScope(handle);
			}
		}
	}
}
