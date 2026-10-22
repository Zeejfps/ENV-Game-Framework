// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the StateMachineMethod table — pairs each compiler-generated state-machine
	/// MoveNext method with the user-written kickoff method (the original async / iterator
	/// declaration). Read directly from the metadata table because System.Reflection.Metadata
	/// doesn't surface these rows through a typed enumeration.
	/// </summary>
	internal sealed class StateMachineMethodTable : MetadataTable<StateMachineMethodTable.StateMachineMethodEntry>
	{
		public StateMachineMethodTable(MetadataFile metadataFile)
			: base(TableIndex.StateMachineMethod, metadataFile)
		{
		}

		protected override IReadOnlyList<StateMachineMethodEntry> LoadTable()
		{
			var list = new List<StateMachineMethodEntry>();
			var metadata = metadataFile.Metadata;
			var length = metadata.GetTableRowCount(TableIndex.StateMachineMethod);
			var reader = metadata.AsBlobReader();
			reader.Offset = metadata.GetTableMetadataOffset(TableIndex.StateMachineMethod);
			int methodDefSize = metadata.GetTableRowSize(TableIndex.StateMachineMethod) / 2;
			for (int rid = 1; rid <= length; rid++)
			{
				var moveNext = MetadataTokens.MethodDefinitionHandle(methodDefSize == 2 ? reader.ReadUInt16() : reader.ReadInt32());
				var kickoff = MetadataTokens.MethodDefinitionHandle(methodDefSize == 2 ? reader.ReadUInt16() : reader.ReadInt32());
				list.Add(new StateMachineMethodEntry(metadataFile, rid, moveNext, kickoff));
			}
			return list;
		}

		public sealed class StateMachineMethodEntry
		{
			readonly MetadataFile metadataFile;
			readonly MethodDefinitionHandle moveNextMethod;
			readonly MethodDefinitionHandle kickoffMethod;

			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x36000000 + RID;

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int MoveNextMethod => MetadataTokens.GetToken(moveNextMethod);

			string? moveNextMethodTooltip;
			public string? MoveNextMethodTooltip => GenerateTooltip(ref moveNextMethodTooltip, metadataFile, moveNextMethod);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int KickoffMethod => MetadataTokens.GetToken(kickoffMethod);

			string? kickoffMethodTooltip;
			public string? KickoffMethodTooltip => GenerateTooltip(ref kickoffMethodTooltip, metadataFile, kickoffMethod);

			public StateMachineMethodEntry(MetadataFile metadataFile, int rid, MethodDefinitionHandle moveNext, MethodDefinitionHandle kickoff)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				moveNextMethod = moveNext;
				kickoffMethod = kickoff;
			}
		}
	}
}
