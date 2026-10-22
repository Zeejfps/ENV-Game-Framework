// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the TypeRef table — every external type the module mentions. Each row points
	/// at the resolution scope (an AssemblyRef, ModuleRef, or another TypeRef for nested
	/// types) plus the namespace + name of the referenced type.
	/// </summary>
	internal sealed class TypeRefTable : MetadataTable<TypeRefTable.TypeRefEntry>
	{
		public TypeRefTable(MetadataFile metadataFile)
			: base(TableIndex.TypeRef, metadataFile)
		{
		}

		protected override IReadOnlyList<TypeRefEntry> LoadTable()
		{
			var list = new List<TypeRefEntry>();
			foreach (var row in metadataFile.Metadata.TypeReferences)
				list.Add(new TypeRefEntry(metadataFile, row));
			return list;
		}

		public sealed class TypeRefEntry
		{
			readonly MetadataFile metadataFile;
			readonly TypeReferenceHandle handle;
			readonly TypeReference typeRef;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.TypeRef, RID);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int ResolutionScope => MetadataTokens.GetToken(typeRef.ResolutionScope);

			string? resolutionScopeTooltip;
			public string? ResolutionScopeTooltip => GenerateTooltip(ref resolutionScopeTooltip, metadataFile, typeRef.ResolutionScope);

			public string Name => metadataFile.Metadata.GetString(typeRef.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(typeRef.Name):X} \"{Name}\"";

			public string Namespace => metadataFile.Metadata.GetString(typeRef.Namespace);

			public string NamespaceTooltip => $"{MetadataTokens.GetHeapOffset(typeRef.Namespace):X} \"{Namespace}\"";

			public TypeRefEntry(MetadataFile metadataFile, TypeReferenceHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				typeRef = metadataFile.Metadata.GetTypeReference(handle);
			}
		}
	}
}
