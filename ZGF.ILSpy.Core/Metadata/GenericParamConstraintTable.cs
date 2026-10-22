// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the GenericParamConstraint table — bounds applied to generic parameters
	/// (<c>where T : SomeBase, IEnumerable&lt;X&gt;</c>). Each row pairs a GenericParam
	/// owner with a TypeDefOrRef coded token naming the constraining type.
	/// </summary>
	internal sealed class GenericParamConstraintTable : MetadataTable<GenericParamConstraintTable.GenericParamConstraintEntry>
	{
		public GenericParamConstraintTable(MetadataFile metadataFile)
			: base(TableIndex.GenericParamConstraint, metadataFile)
		{
		}

		protected override IReadOnlyList<GenericParamConstraintEntry> LoadTable()
		{
			var list = new List<GenericParamConstraintEntry>();
			var metadata = metadataFile.Metadata;
			for (int row = 1; row <= metadata.GetTableRowCount(TableIndex.GenericParamConstraint); row++)
				list.Add(new GenericParamConstraintEntry(metadataFile, MetadataTokens.GenericParameterConstraintHandle(row)));
			return list;
		}

		public sealed class GenericParamConstraintEntry
		{
			readonly MetadataFile metadataFile;
			readonly GenericParameterConstraintHandle handle;
			readonly GenericParameterConstraint genericParamConstraint;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.GenericParamConstraint, RID);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Owner => MetadataTokens.GetToken(genericParamConstraint.Parameter);

			string? ownerTooltip;
			public string? OwnerTooltip {
				get {
					if (ownerTooltip == null)
					{
						ITextOutput output = new PlainTextOutput();
						var p = metadataFile.Metadata.GetGenericParameter(genericParamConstraint.Parameter);
						output.Write("parameter " + p.Index + (p.Name.IsNil ? "" : " (" + metadataFile.Metadata.GetString(p.Name) + ")") + " of ");
						p.Parent.WriteTo(metadataFile, output, default);
						ownerTooltip = output.ToString();
					}
					return ownerTooltip;
				}
			}

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Type => MetadataTokens.GetToken(genericParamConstraint.Type);

			string? typeTooltip;
			public string? TypeTooltip => GenerateTooltip(ref typeTooltip, metadataFile, genericParamConstraint.Type);

			public GenericParamConstraintEntry(MetadataFile metadataFile, GenericParameterConstraintHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				genericParamConstraint = metadataFile.Metadata.GetGenericParameterConstraint(handle);
			}
		}
	}
}
