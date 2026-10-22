// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the ExportedType table — types this assembly forwards to (or re-exports
	/// from) another assembly. Common in type-forwarding scenarios where mscorlib.dll
	/// exposes types that physically live in System.Runtime.dll.
	/// </summary>
	internal sealed class ExportedTypeTable : MetadataTable<ExportedTypeTable.ExportedTypeEntry>
	{
		public ExportedTypeTable(MetadataFile metadataFile)
			: base(TableIndex.ExportedType, metadataFile)
		{
		}

		protected override IReadOnlyList<ExportedTypeEntry> LoadTable()
		{
			var list = new List<ExportedTypeEntry>();
			var metadata = metadataFile.Metadata;
			foreach (var row in metadata.ExportedTypes)
				list.Add(new ExportedTypeEntry(metadataFile, row, metadata.GetExportedType(row)));
			return list;
		}

		public sealed class ExportedTypeEntry
		{
			readonly MetadataFile metadataFile;
			readonly ExportedTypeHandle handle;
			readonly ExportedType type;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.ExportedType, RID);

			[ColumnInfo("X8")]
			public TypeAttributes Attributes => type.Attributes;

			public object AttributesTooltip => FlagsTooltip.ForTypeAttributes(type.Attributes);

			[ColumnInfo("X8")]
			public int TypeDefId => type.GetTypeDefinitionId();

			public string TypeNameTooltip => $"{MetadataTokens.GetHeapOffset(type.Name):X} \"{TypeName}\"";

			public string TypeName => metadataFile.Metadata.GetString(type.Name);

			public string TypeNamespaceTooltip => $"{MetadataTokens.GetHeapOffset(type.Namespace):X} \"{TypeNamespace}\"";

			public string TypeNamespace => metadataFile.Metadata.GetString(type.Namespace);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Implementation => MetadataTokens.GetToken(type.Implementation);

			string? implementationTooltip;
			public string? ImplementationTooltip => GenerateTooltip(ref implementationTooltip, metadataFile, type.Implementation);

			public ExportedTypeEntry(MetadataFile metadataFile, ExportedTypeHandle handle, ExportedType type)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				this.type = type;
			}
		}
	}
}
