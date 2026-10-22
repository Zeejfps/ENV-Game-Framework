// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the PropertyMap table — pairs each TypeDef that declares properties with the
	/// first row of its property list. Same shape as EventMap but for properties.
	/// </summary>
	internal sealed class PropertyMapTable : MetadataTable<PropertyMapTable.PropertyMapEntry>
	{
		public PropertyMapTable(MetadataFile metadataFile)
			: base(TableIndex.PropertyMap, metadataFile)
		{
		}

		protected override IReadOnlyList<PropertyMapEntry> LoadTable()
		{
			var list = new List<PropertyMapEntry>();
			foreach (var (parent, propertyList) in metadataFile.Metadata.GetPropertyMaps())
				list.Add(new PropertyMapEntry(metadataFile, list.Count + 1, parent, propertyList));
			return list;
		}

		public sealed class PropertyMapEntry
		{
			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x15000000 | RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Parent => MetadataTokens.GetToken(parent);

			string? parentTooltip;
			public string? ParentTooltip => GenerateTooltip(ref parentTooltip, metadataFile, parent);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int PropertyList => MetadataTokens.GetToken(propertyList);

			string? propertyListTooltip;
			public string? PropertyListTooltip => GenerateTooltip(ref propertyListTooltip, metadataFile, propertyList);

			readonly MetadataFile metadataFile;
			readonly TypeDefinitionHandle parent;
			readonly PropertyDefinitionHandle propertyList;

			public PropertyMapEntry(MetadataFile metadataFile, int rid, TypeDefinitionHandle parent, PropertyDefinitionHandle propertyList)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				this.parent = parent;
				this.propertyList = propertyList;
			}
		}
	}
}
