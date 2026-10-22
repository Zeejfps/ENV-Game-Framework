// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the InterfaceImpl table — every type/interface implementation pair. Class is
	/// a TypeDef row; Interface is a TypeDefOrRef coded token (TypeDef / TypeRef / TypeSpec).
	/// </summary>
	internal sealed class InterfaceImplTable : MetadataTable<InterfaceImplTable.InterfaceImplEntry>
	{
		public InterfaceImplTable(MetadataFile metadataFile)
			: base(TableIndex.InterfaceImpl, metadataFile)
		{
		}

		protected override IReadOnlyList<InterfaceImplEntry> LoadTable()
		{
			var list = new List<InterfaceImplEntry>();
			foreach (var (@class, @interface) in metadataFile.Metadata.GetInterfaceImplRows())
				list.Add(new InterfaceImplEntry(metadataFile, list.Count + 1, @class, @interface));
			return list;
		}

		public sealed class InterfaceImplEntry
		{
			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x09000000 | RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Class => MetadataTokens.GetToken(@class);

			string? classTooltip;
			public string? ClassTooltip => GenerateTooltip(ref classTooltip, metadataFile, @class);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Interface => MetadataTokens.GetToken(@interface);

			string? interfaceTooltip;
			public string? InterfaceTooltip => GenerateTooltip(ref interfaceTooltip, metadataFile, @interface);

			readonly MetadataFile metadataFile;
			readonly TypeDefinitionHandle @class;
			readonly EntityHandle @interface;

			public InterfaceImplEntry(MetadataFile metadataFile, int rid, TypeDefinitionHandle @class, EntityHandle @interface)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				this.@class = @class;
				this.@interface = @interface;
			}
		}
	}
}
