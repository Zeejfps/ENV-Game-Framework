// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the LocalScope table — every lexical scope inside every method body's
	/// debug info. Each row points back at the MethodDef + import-scope + local-variable
	/// list and carries the scope's IL start offset and length.
	/// </summary>
	internal sealed class LocalScopeTable : MetadataTable<LocalScopeTable.LocalScopeEntry>
	{
		public LocalScopeTable(MetadataFile metadataFile)
			: base(TableIndex.LocalScope, metadataFile)
		{
		}

		protected override IReadOnlyList<LocalScopeEntry> LoadTable()
		{
			var list = new List<LocalScopeEntry>();
			foreach (var row in metadataFile.Metadata.LocalScopes)
				list.Add(new LocalScopeEntry(metadataFile, row));
			return list;
		}

		public sealed class LocalScopeEntry
		{
			readonly MetadataFile metadataFile;
			readonly LocalScopeHandle handle;
			readonly LocalScope localScope;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Method => MetadataTokens.GetToken(localScope.Method);

			string? methodTooltip;
			public string? MethodTooltip => GenerateTooltip(ref methodTooltip, metadataFile, localScope.Method);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int ImportScope => MetadataTokens.GetToken(localScope.ImportScope);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int VariableList => MetadataTokens.GetToken(localScope.GetLocalVariables().FirstOrDefault());

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int ConstantList => MetadataTokens.GetToken(localScope.GetLocalConstants().FirstOrDefault());

			public int StartOffset => localScope.StartOffset;

			public int Length => localScope.Length;

			public LocalScopeEntry(MetadataFile metadataFile, LocalScopeHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				localScope = metadataFile.Metadata.GetLocalScope(handle);
			}
		}
	}
}
