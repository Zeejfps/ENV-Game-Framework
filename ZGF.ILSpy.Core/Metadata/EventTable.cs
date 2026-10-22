// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the Event table — every CLR event the module's types declare. Each row
	/// references the event's delegate type as a TypeDefOrRef coded token; add/remove/raise
	/// accessor bindings live in MethodSemantics.
	/// </summary>
	internal sealed class EventTable : MetadataTable<EventTable.EventDefEntry>
	{
		public EventTable(MetadataFile metadataFile)
			: base(TableIndex.Event, metadataFile)
		{
		}

		protected override IReadOnlyList<EventDefEntry> LoadTable()
		{
			var list = new List<EventDefEntry>();
			foreach (var row in metadataFile.Metadata.EventDefinitions)
				list.Add(new EventDefEntry(metadataFile, row));
			return list;
		}

		public sealed class EventDefEntry
		{
			readonly MetadataFile metadataFile;
			readonly EventDefinitionHandle handle;
			readonly EventDefinition eventDef;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.Event, RID);

			[ColumnInfo("X8")]
			public EventAttributes Attributes => eventDef.Attributes;

			public object AttributesTooltip => new FlagsTooltip {
				FlagGroup.CreateMultipleChoiceGroup(typeof(EventAttributes), selectedValue: (int)eventDef.Attributes, includeAll: false),
			};

			public string Name => metadataFile.Metadata.GetString(eventDef.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(eventDef.Name):X} \"{Name}\"";

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Type => MetadataTokens.GetToken(eventDef.Type);

			string? typeTooltip;
			public string? TypeTooltip => GenerateTooltip(ref typeTooltip, metadataFile, eventDef.Type);

			public EventDefEntry(MetadataFile metadataFile, EventDefinitionHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				eventDef = metadataFile.Metadata.GetEventDefinition(handle);
			}
		}
	}
}
