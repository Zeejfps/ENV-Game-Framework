// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the Field table — every field defined by the module's types. Each row carries
	/// the field's attributes (visibility / static / const / …), name, and the heap offset
	/// of its signature blob.
	/// </summary>
	internal sealed class FieldTable : MetadataTable<FieldTable.FieldDefEntry>
	{
		public FieldTable(MetadataFile metadataFile)
			: base(TableIndex.Field, metadataFile)
		{
		}

		protected override IReadOnlyList<FieldDefEntry> LoadTable()
		{
			var list = new List<FieldDefEntry>();
			foreach (var row in metadataFile.Metadata.FieldDefinitions)
				list.Add(new FieldDefEntry(metadataFile, row));
			return list;
		}

		public sealed class FieldDefEntry
		{
			readonly MetadataFile metadataFile;
			readonly FieldDefinitionHandle handle;
			readonly FieldDefinition fieldDef;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.Field, RID);

			[ColumnInfo("X8")]
			public FieldAttributes Attributes => fieldDef.Attributes;

			const FieldAttributes otherFlagsMask = ~(FieldAttributes.FieldAccessMask);

			public object AttributesTooltip => new FlagsTooltip() {
				FlagGroup.CreateSingleChoiceGroup(typeof(FieldAttributes), "Field access: ", (int)FieldAttributes.FieldAccessMask, (int)(fieldDef.Attributes & FieldAttributes.FieldAccessMask), new Flag("CompilerControlled (0000)", 0, false), includeAny: false),
				FlagGroup.CreateMultipleChoiceGroup(typeof(FieldAttributes), "Flags:", (int)otherFlagsMask, (int)(fieldDef.Attributes & otherFlagsMask), includeAll: false),
			};

			public string Name => metadataFile.Metadata.GetString(fieldDef.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(fieldDef.Name):X} \"{Name}\"";

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Signature => MetadataTokens.GetHeapOffset(fieldDef.Signature);

			string? signatureTooltip;
			public string? SignatureTooltip => GenerateTooltip(ref signatureTooltip, metadataFile, handle);

			public FieldDefEntry(MetadataFile metadataFile, FieldDefinitionHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				fieldDef = metadataFile.Metadata.GetFieldDefinition(handle);
			}
		}
	}
}
