// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the EventMap table — pairs each TypeDef that declares events with the first
	/// row of its event list (later rows belong to the same type until the next EventMap row).
	/// </summary>
	internal sealed class EventMapTable : MetadataTable<EventMapTable.EventMapEntry>
	{
		public EventMapTable(MetadataFile metadataFile)
			: base(TableIndex.EventMap, metadataFile)
		{
		}

		protected override IReadOnlyList<EventMapEntry> LoadTable()
		{
			var list = new List<EventMapEntry>();
			foreach (var (parent, eventList) in metadataFile.Metadata.GetEventMaps())
				list.Add(new EventMapEntry(metadataFile, list.Count + 1, parent, eventList));
			return list;
		}

		public sealed class EventMapEntry
		{
			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x12000000 | RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Parent => MetadataTokens.GetToken(parent);

			string? parentTooltip;
			public string? ParentTooltip => GenerateTooltip(ref parentTooltip, metadataFile, parent);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int EventList => MetadataTokens.GetToken(eventList);

			string? eventListTooltip;
			public string? EventListTooltip => GenerateTooltip(ref eventListTooltip, metadataFile, eventList);

			readonly MetadataFile metadataFile;
			readonly TypeDefinitionHandle parent;
			readonly EventDefinitionHandle eventList;

			public EventMapEntry(MetadataFile metadataFile, int rid, TypeDefinitionHandle parent, EventDefinitionHandle eventList)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				this.parent = parent;
				this.eventList = eventList;
			}
		}
	}
}
