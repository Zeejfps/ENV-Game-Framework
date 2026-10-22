// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the DeclSecurity table — declarative security attributes (CAS-era
	/// <c>[SecurityPermission(...)]</c>) attached to assemblies, types, or methods. The
	/// PermissionSet blob holds a serialised XML / binary permission descriptor.
	/// </summary>
	internal sealed class DeclSecurityTable : MetadataTable<DeclSecurityTable.DeclSecurityEntry>
	{
		public DeclSecurityTable(MetadataFile metadataFile)
			: base(TableIndex.DeclSecurity, metadataFile)
		{
		}

		protected override IReadOnlyList<DeclSecurityEntry> LoadTable()
		{
			var list = new List<DeclSecurityEntry>();
			foreach (var row in metadataFile.Metadata.DeclarativeSecurityAttributes)
				list.Add(new DeclSecurityEntry(metadataFile, row));
			return list;
		}

		public sealed class DeclSecurityEntry
		{
			readonly MetadataFile metadataFile;
			readonly DeclarativeSecurityAttributeHandle handle;
			readonly DeclarativeSecurityAttribute declSecAttr;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.DeclSecurity, RID);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Parent => MetadataTokens.GetToken(declSecAttr.Parent);

			string? parentTooltip;
			public string? ParentTooltip => GenerateTooltip(ref parentTooltip, metadataFile, declSecAttr.Parent);

			[ColumnInfo("X8")]
			public DeclarativeSecurityAction Action => declSecAttr.Action;

			public string ActionTooltip {
				get {
					return declSecAttr.Action.ToString();
				}
			}

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int PermissionSet => MetadataTokens.GetHeapOffset(declSecAttr.PermissionSet);

			public string? PermissionSetTooltip {
				get {
					return null;
				}
			}

			public DeclSecurityEntry(MetadataFile metadataFile, DeclarativeSecurityAttributeHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				declSecAttr = metadataFile.Metadata.GetDeclarativeSecurityAttribute(handle);
			}
		}
	}
}
