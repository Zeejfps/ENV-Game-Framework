// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the AssemblyRef table — every external assembly the module imports. Each row
	/// carries the reference's name, version, culture, and the public-key-or-token blob
	/// that pins it to a specific signing identity.
	/// </summary>
	internal sealed class AssemblyRefTable : MetadataTable<AssemblyRefTable.AssemblyRefEntry>
	{
		public AssemblyRefTable(MetadataFile metadataFile)
			: base(TableIndex.AssemblyRef, metadataFile)
		{
		}

		protected override IReadOnlyList<AssemblyRefEntry> LoadTable()
		{
			var list = new List<AssemblyRefEntry>();
			foreach (var row in metadataFile.Metadata.AssemblyReferences)
				list.Add(new AssemblyRefEntry(metadataFile, row));
			return list;
		}

		public sealed class AssemblyRefEntry
		{
			readonly MetadataFile metadataFile;
			readonly AssemblyReferenceHandle handle;
			readonly System.Reflection.Metadata.AssemblyReference assemblyRef;

			public int RID => MetadataTokens.GetRowNumber(handle);

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8")]
			public int Offset => GetRowOffset(metadataFile, TableIndex.AssemblyRef, RID);

			public Version Version => assemblyRef.Version;

			[ColumnInfo("X8")]
			public AssemblyFlags Flags => assemblyRef.Flags;

			public object FlagsTooltip => new FlagsTooltip((int)assemblyRef.Flags, null) {
				FlagGroup.CreateMultipleChoiceGroup(typeof(AssemblyFlags), selectedValue: (int)assemblyRef.Flags, includeAll: false)
			};

			[ColumnInfo("X8", Kind = ColumnKind.HeapOffset)]
			public int PublicKeyOrToken => MetadataTokens.GetHeapOffset(assemblyRef.PublicKeyOrToken);

			public string? PublicKeyOrTokenTooltip {
				get {
					if (assemblyRef.PublicKeyOrToken.IsNil)
						return null;
					System.Collections.Immutable.ImmutableArray<byte> token = metadataFile.Metadata.GetBlobContent(assemblyRef.PublicKeyOrToken);
					return token.ToHexString(token.Length);
				}
			}

			public string NameTooltip => $"{MetadataTokens.GetHeapOffset(assemblyRef.Name):X} \"{Name}\"";

			public string Name => metadataFile.Metadata.GetString(assemblyRef.Name);

			public string CultureTooltip => $"{MetadataTokens.GetHeapOffset(assemblyRef.Culture):X} \"{Culture}\"";

			public string Culture => metadataFile.Metadata.GetString(assemblyRef.Culture);

			public AssemblyRefEntry(MetadataFile metadataFile, AssemblyReferenceHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				assemblyRef = metadataFile.Metadata.GetAssemblyReference(handle);
			}
		}
	}
}
