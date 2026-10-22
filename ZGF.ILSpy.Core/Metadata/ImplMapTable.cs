// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the ImplMap table — P/Invoke linkage. Each row maps a managed MethodDef (or
	/// Field, in legacy CIL) to its native entry point: the <c>extern</c> name plus the
	/// ModuleRef holding the DLL identifier.
	/// </summary>
	internal sealed class ImplMapTable : MetadataTable<ImplMapTable.ImplMapEntry>
	{
		public ImplMapTable(MetadataFile metadataFile)
			: base(TableIndex.ImplMap, metadataFile)
		{
		}

		protected override IReadOnlyList<ImplMapEntry> LoadTable()
		{
			var list = new List<ImplMapEntry>();
			int rid = 0;
			foreach (var (mappingFlags, memberForwarded, importName, importScope) in metadataFile.Metadata.GetImplMaps())
			{
				list.Add(new ImplMapEntry(metadataFile, ++rid, mappingFlags, memberForwarded, importName, importScope));
			}
			return list;
		}

		public sealed class ImplMapEntry
		{
			public int RID { get; }

			[ColumnInfo("X8")]
			public int Token => 0x1C000000 | RID;

			[ColumnInfo("X8")]
			public MethodImportAttributes MappingFlags { get; }

			const MethodImportAttributes otherFlagsMask = ~(MethodImportAttributes.CallingConventionMask | MethodImportAttributes.CharSetMask);

			public object MappingFlagsTooltip => new FlagsTooltip {
				FlagGroup.CreateSingleChoiceGroup(typeof(MethodImportAttributes), "Character set: ", (int)MethodImportAttributes.CharSetMask, (int)(MappingFlags & MethodImportAttributes.CharSetMask), new Flag("CharSetNotSpec (0000)", 0, false), includeAny: false),
				FlagGroup.CreateSingleChoiceGroup(typeof(MethodImportAttributes), "Calling convention: ", (int)MethodImportAttributes.CallingConventionMask, (int)(MappingFlags & MethodImportAttributes.CallingConventionMask), new Flag("Invalid (0000)", 0, false), includeAny: false),
				FlagGroup.CreateMultipleChoiceGroup(typeof(MethodImportAttributes), "Flags:", (int)otherFlagsMask, (int)(MappingFlags & otherFlagsMask), includeAll: false),
			};

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int MemberForwarded => MetadataTokens.GetToken(memberForwarded);

			string? memberForwardedTooltip;
			public string? MemberForwardedTooltip => GenerateTooltip(ref memberForwardedTooltip, metadataFile, memberForwarded);

			public string ImportName { get; }

			public string ImportNameTooltip => $"{MetadataTokens.GetHeapOffset(importNameHandle):X} \"{ImportName}\"";

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int ImportScope => MetadataTokens.GetToken(importScope);

			string? importScopeTooltip;
			public string? ImportScopeTooltip => GenerateTooltip(ref importScopeTooltip, metadataFile, importScope);

			readonly MetadataFile metadataFile;
			readonly EntityHandle memberForwarded;
			readonly StringHandle importNameHandle;
			readonly ModuleReferenceHandle importScope;

			public ImplMapEntry(MetadataFile metadataFile, int rid, MethodImportAttributes mappingFlags, EntityHandle memberForwarded, StringHandle importNameHandle, ModuleReferenceHandle importScope)
			{
				this.metadataFile = metadataFile;
				RID = rid;
				MappingFlags = mappingFlags;
				this.memberForwarded = memberForwarded;
				this.importNameHandle = importNameHandle;
				ImportName = metadataFile.Metadata.GetString(importNameHandle);
				this.importScope = importScope;
			}
		}
	}
}
