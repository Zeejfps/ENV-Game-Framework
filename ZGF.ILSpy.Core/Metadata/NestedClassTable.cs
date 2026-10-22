// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the NestedClass table — pairs each nested type with its enclosing type.
	/// </summary>
	internal sealed class NestedClassTable : MetadataTable<NestedClassTable.NestedClassEntry>
	{
		public NestedClassTable(MetadataFile metadataFile)
			: base(TableIndex.NestedClass, metadataFile)
		{
		}

		protected override IReadOnlyList<NestedClassEntry> LoadTable()
		{
			var list = new List<NestedClassEntry>();
			foreach (var (nested, enclosing) in metadataFile.Metadata.GetNestedClasses())
				list.Add(new NestedClassEntry(metadataFile, list.Count + 1, nested, enclosing));
			return list;
		}

		public sealed class NestedClassEntry
		{
			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x29000000 | RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int NestedClass => MetadataTokens.GetToken(nested);

			string? nestedClassTooltip;
			public string? NestedClassTooltip => GenerateTooltip(ref nestedClassTooltip, metadataFile, nested);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int EnclosingClass => MetadataTokens.GetToken(enclosing);

			string? enclosingClassTooltip;
			public string? EnclosingClassTooltip => GenerateTooltip(ref enclosingClassTooltip, metadataFile, enclosing);

			readonly MetadataFile metadataFile;
			readonly TypeDefinitionHandle nested;
			readonly TypeDefinitionHandle enclosing;

			public NestedClassEntry(MetadataFile metadataFile, int rid, TypeDefinitionHandle nested, TypeDefinitionHandle enclosing)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				this.nested = nested;
				this.enclosing = enclosing;
			}
		}
	}
}
