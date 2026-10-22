// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the StandAloneSig table — signatures referenced from outside their natural
	/// owners: local-variable layouts for method bodies and call-site signatures for
	/// <c>calli</c>. Each row carries a single blob signature handle.
	/// </summary>
	internal sealed class StandAloneSigTable : MetadataTable<StandAloneSigTable.StandAloneSigEntry>
	{
		public StandAloneSigTable(MetadataFile metadataFile)
			: base(TableIndex.StandAloneSig, metadataFile)
		{
		}

		protected override IReadOnlyList<StandAloneSigEntry> LoadTable()
		{
			var list = new List<StandAloneSigEntry>();
			for (int row = 1; row <= metadataFile.Metadata.GetTableRowCount(TableIndex.StandAloneSig); row++)
				list.Add(new StandAloneSigEntry(metadataFile, MetadataTokens.StandaloneSignatureHandle(row)));
			return list;
		}

		public sealed class StandAloneSigEntry
		{
			readonly MetadataFile metadataFile;
			readonly StandaloneSignatureHandle handle;
			readonly StandaloneSignature standaloneSig;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.StandAloneSig, RID);

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Signature => MetadataTokens.GetHeapOffset(standaloneSig.Signature);

			string? signatureTooltip;
			public string? SignatureTooltip => GenerateTooltip(ref signatureTooltip, metadataFile, handle);

			public StandAloneSigEntry(MetadataFile metadataFile, StandaloneSignatureHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				standaloneSig = metadataFile.Metadata.GetStandaloneSignature(handle);
			}
		}
	}
}
