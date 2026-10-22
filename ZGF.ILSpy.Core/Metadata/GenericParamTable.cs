// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the GenericParam table — every type or method generic-parameter slot. Owner
	/// is a TypeOrMethodDef coded token; Number is the parameter's 0-based index inside
	/// its owner's type-parameter list.
	/// </summary>
	internal sealed class GenericParamTable : MetadataTable<GenericParamTable.GenericParamEntry>
	{
		public GenericParamTable(MetadataFile metadataFile)
			: base(TableIndex.GenericParam, metadataFile)
		{
		}

		protected override IReadOnlyList<GenericParamEntry> LoadTable()
		{
			var list = new List<GenericParamEntry>();
			for (int row = 1; row <= metadataFile.Metadata.GetTableRowCount(TableIndex.GenericParam); row++)
				list.Add(new GenericParamEntry(metadataFile, MetadataTokens.GenericParameterHandle(row)));
			return list;
		}

		public sealed class GenericParamEntry
		{
			readonly MetadataFile metadataFile;
			readonly GenericParameterHandle handle;
			readonly GenericParameter genericParam;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.GenericParam, RID);

			public int Number => genericParam.Index;

			[ColumnInfo("X8")]
			public GenericParameterAttributes Attributes => genericParam.Attributes;

			public object AttributesTooltip => new FlagsTooltip {
				FlagGroup.CreateSingleChoiceGroup(typeof(GenericParameterAttributes), "Variance: ", (int)GenericParameterAttributes.VarianceMask, (int)(genericParam.Attributes & GenericParameterAttributes.VarianceMask), new Flag("None (0000)", 0, false), includeAny: false),
				FlagGroup.CreateMultipleChoiceGroup(typeof(GenericParameterAttributes), "Special Constraint: ", (int)GenericParameterAttributes.SpecialConstraintMask, (int)(genericParam.Attributes & GenericParameterAttributes.SpecialConstraintMask), includeAll: false),
			};

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Owner => MetadataTokens.GetToken(genericParam.Parent);

			string? ownerTooltip;
			public string? OwnerTooltip => GenerateTooltip(ref ownerTooltip, metadataFile, genericParam.Parent);

			public string Name => metadataFile.Metadata.GetString(genericParam.Name);

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(genericParam.Name):X} \"{Name}\"";

			public GenericParamEntry(MetadataFile metadataFile, GenericParameterHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				genericParam = metadataFile.Metadata.GetGenericParameter(handle);
			}
		}
	}
}
