// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the Property table — every property the module's types declare. Method-level
	/// accessor binding lives in MethodSemantics; this table just carries each property's
	/// attributes, name, and signature blob.
	/// </summary>
	internal sealed class PropertyTable : MetadataTable<PropertyTable.PropertyDefEntry>
	{
		public PropertyTable(MetadataFile metadataFile)
			: base(TableIndex.Property, metadataFile)
		{
		}

		protected override IReadOnlyList<PropertyDefEntry> LoadTable()
		{
			var list = new List<PropertyDefEntry>();
			foreach (var row in metadataFile.Metadata.PropertyDefinitions)
				list.Add(new PropertyDefEntry(metadataFile, row));
			return list;
		}

		public sealed class PropertyDefEntry
		{
			readonly MetadataFile metadataFile;
			readonly PropertyDefinitionHandle handle;
			readonly PropertyDefinition propertyDef;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.Property, RID);

			[ColumnInfo("X8")]
			public PropertyAttributes Attributes => propertyDef.Attributes;

			public object AttributesTooltip => new FlagsTooltip {
				FlagGroup.CreateMultipleChoiceGroup(typeof(PropertyAttributes), selectedValue: (int)propertyDef.Attributes, includeAll: false),
			};

			public string Name => metadataFile.Metadata.GetString(propertyDef.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(propertyDef.Name):X} \"{Name}\"";

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Signature => MetadataTokens.GetHeapOffset(propertyDef.Signature);

			string? signatureTooltip;
			public string? SignatureTooltip => GenerateTooltip(ref signatureTooltip, metadataFile, handle);

			public PropertyDefEntry(MetadataFile metadataFile, PropertyDefinitionHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				propertyDef = metadataFile.Metadata.GetPropertyDefinition(handle);
			}
		}
	}
}
