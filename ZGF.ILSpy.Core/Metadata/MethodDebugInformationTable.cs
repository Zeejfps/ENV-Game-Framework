// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the MethodDebugInformation table — sequence-point and local-signature data
	/// per method body. Each row links a MethodDef token to the document containing its
	/// source and the blob holding its sequence-points list.
	/// </summary>
	internal sealed class MethodDebugInformationTable : MetadataTable<MethodDebugInformationTable.MethodDebugInformationEntry>
	{
		public MethodDebugInformationTable(MetadataFile metadataFile)
			: base(TableIndex.MethodDebugInformation, metadataFile)
		{
		}

		protected override IReadOnlyList<MethodDebugInformationEntry> LoadTable()
		{
			var list = new List<MethodDebugInformationEntry>();
			foreach (var row in metadataFile.Metadata.MethodDebugInformation)
				list.Add(new MethodDebugInformationEntry(metadataFile, row));
			return list;
		}

		public sealed class MethodDebugInformationEntry
		{
			readonly MetadataFile metadataFile;
			readonly MethodDebugInformationHandle handle;
			readonly MethodDebugInformation debugInfo;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Document => MetadataTokens.GetToken(debugInfo.Document);

			public string? DocumentTooltip {
				get {
					if (debugInfo.Document.IsNil)
						return null;
					var document = metadataFile.Metadata.GetDocument(debugInfo.Document);
					return $"{MetadataTokens.GetHeapOffset(document.Name):X} \"{metadataFile.Metadata.GetString(document.Name)}\"";
				}
			}

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int SequencePoints => MetadataTokens.GetHeapOffset(debugInfo.SequencePointsBlob);

			public string? SequencePointsTooltip {
				get {
					if (debugInfo.SequencePointsBlob.IsNil)
						return null;
					StringBuilder sb = new StringBuilder();
					foreach (var p in debugInfo.GetSequencePoints())
					{
						sb.AppendLine($"document='{MetadataTokens.GetToken(p.Document):X8}', offset={p.Offset}, start={p.StartLine};{p.StartColumn}, end={p.EndLine};{p.EndColumn}, hidden={p.IsHidden}");
					}
					return sb.ToString().TrimEnd();
				}
			}

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int LocalSignature => MetadataTokens.GetToken(debugInfo.LocalSignature);

			public string? LocalSignatureTooltip {
				get {
					if (debugInfo.LocalSignature.IsNil)
						return null;
					ITextOutput output = new PlainTextOutput();
					var context = new MetadataGenericContext(default(TypeDefinitionHandle), metadataFile.Metadata);
					StandaloneSignature localSignature = metadataFile.Metadata.GetStandaloneSignature(debugInfo.LocalSignature);
					var signatureDecoder = new DisassemblerSignatureTypeProvider(metadataFile, output);
					int index = 0;
					foreach (var item in localSignature.DecodeLocalSignature(signatureDecoder, context))
					{
						if (index > 0)
							output.WriteLine();
						output.Write("[{0}] ", index);
						item(ILNameSyntax.Signature);
						index++;
					}
					return output.ToString();
				}
			}

			public MethodDebugInformationEntry(MetadataFile metadataFile, MethodDebugInformationHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				debugInfo = metadataFile.Metadata.GetMethodDebugInformation(handle);
			}
		}
	}
}
