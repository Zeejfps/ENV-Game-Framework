// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the CustomAttribute table — every <c>[Attr(...)]</c> annotation attached to
	/// every metadata row. Parent is a HasCustomAttribute coded token; Constructor points at
	/// the attribute class's ctor (MethodDef or MemberRef); Value blob holds the constructor
	/// arguments + named properties as a serialised payload.
	/// </summary>
	internal sealed class CustomAttributeTable : MetadataTable<CustomAttributeTable.CustomAttributeEntry>
	{
		public CustomAttributeTable(MetadataFile metadataFile)
			: base(TableIndex.CustomAttribute, metadataFile)
		{
		}

		protected override IReadOnlyList<CustomAttributeEntry> LoadTable()
		{
			var list = new List<CustomAttributeEntry>();
			foreach (var row in metadataFile.Metadata.CustomAttributes)
				list.Add(new CustomAttributeEntry(metadataFile, row));
			return list;
		}

		public sealed class CustomAttributeEntry
		{
			readonly MetadataFile metadataFile;
			readonly CustomAttributeHandle handle;
			readonly CustomAttribute customAttr;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.CustomAttribute, RID);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Parent => MetadataTokens.GetToken(customAttr.Parent);

			string? parentTooltip;
			public string? ParentTooltip => GenerateTooltip(ref parentTooltip, metadataFile, customAttr.Parent);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Constructor => MetadataTokens.GetToken(customAttr.Constructor);

			string? constructorTooltip;
			public string? ConstructorTooltip => GenerateTooltip(ref constructorTooltip, metadataFile, customAttr.Constructor);

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int Value => MetadataTokens.GetHeapOffset(customAttr.Value);

			public string? ValueTooltip {
				get {
					return null;
				}
			}

			public CustomAttributeEntry(MetadataFile metadataFile, CustomAttributeHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				customAttr = metadataFile.Metadata.GetCustomAttribute(handle);
			}
		}
	}
}
