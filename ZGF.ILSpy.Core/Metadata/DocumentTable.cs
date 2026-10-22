// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the Document table — every source file referenced by debug info. Each row
	/// carries the path string, plus heap-offsets for hash algorithm, hash bytes, and
	/// language GUID.
	/// </summary>
	internal sealed class DocumentTable : MetadataTable<DocumentTable.DocumentEntry>
	{
		public DocumentTable(MetadataFile metadataFile)
			: base(TableIndex.Document, metadataFile)
		{
		}

		protected override IReadOnlyList<DocumentEntry> LoadTable()
		{
			var list = new List<DocumentEntry>();
			foreach (var row in metadataFile.Metadata.Documents)
				list.Add(new DocumentEntry(metadataFile, row));
			return list;
		}

		public sealed class DocumentEntry
		{
			readonly MetadataFile metadataFile;
			readonly DocumentHandle handle;
			readonly Document document;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			public string Name => metadataFile.Metadata.GetString(document.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(document.Name):X} \"{Name}\"";

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int HashAlgorithm => MetadataTokens.GetHeapOffset(document.HashAlgorithm);

			public string? HashAlgorithmTooltip {
				get {
					if (document.HashAlgorithm.IsNil)
						return null;
					Guid guid = metadataFile.Metadata.GetGuid(document.HashAlgorithm);
					if (guid == KnownGuids.HashAlgorithmSHA1)
						return "SHA1 [ff1816ec-aa5e-4d10-87f7-6f4963833460]";
					if (guid == KnownGuids.HashAlgorithmSHA256)
						return "SHA256 [8829d00f-11b8-4213-878b-770e8597ac16]";
					return $"Unknown [" + guid + "]";
				}
			}

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Hash => MetadataTokens.GetHeapOffset(document.Hash);

			public string? HashTooltip {
				get {
					if (document.Hash.IsNil)
						return null;
					System.Collections.Immutable.ImmutableArray<byte> token = metadataFile.Metadata.GetBlobContent(document.Hash);
					return token.ToHexString(token.Length);
				}
			}

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Language => MetadataTokens.GetHeapOffset(document.Language);

			public string? LanguageTooltip {
				get {
					if (document.Language.IsNil)
						return null;
					Guid guid = metadataFile.Metadata.GetGuid(document.Language);
					if (guid == KnownGuids.CSharpLanguageGuid)
						return "Visual C# [3f5162f8-07c6-11d3-9053-00c04fa302a1]";
					if (guid == KnownGuids.VBLanguageGuid)
						return "Visual Basic [3a12d0b8-c26c-11d0-b442-00a0244a1dd2]";
					if (guid == KnownGuids.FSharpLanguageGuid)
						return "Visual F# [ab4f38c9-b6e6-43ba-be3b-58080b2ccce3]";
					return $"Unknown [" + guid + "]";
				}
			}

			public DocumentEntry(MetadataFile metadataFile, DocumentHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				document = metadataFile.Metadata.GetDocument(handle);
			}
		}
	}
}
