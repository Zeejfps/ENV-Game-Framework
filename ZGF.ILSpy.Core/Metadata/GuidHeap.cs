// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of #GUID — a contiguous array of 16-byte GUIDs indexed (1-based) by ModuleDef
	/// rows and a handful of other tables. Indexing starts at 1 because index 0 is reserved
	/// for "no GUID".
	/// </summary>
	internal sealed class GuidHeap : MetadataHeap<GuidHeap.GuidHeapEntry>
	{
		public GuidHeap(MetadataFile metadataFile)
			: base(HandleKind.Guid, metadataFile)
		{
		}

		protected override string HeapName => "Guid Heap";

		protected override void LoadEntries(MetadataReader metadata, List<GuidHeapEntry> list)
		{
			int count = metadata.GetHeapSize(HeapIndex.Guid) >> 4;
			for (int i = 1; i <= count; i++)
				list.Add(new GuidHeapEntry(metadata, MetadataTokens.GuidHandle(i)));
		}

		public sealed class GuidHeapEntry
		{
			readonly MetadataReader metadata;
			readonly GuidHandle handle;

			public int Index => metadata.GetHeapOffset(handle);
			public int Length => 16;
			public string Value => metadata.GetGuid(handle).ToString();

			public GuidHeapEntry(MetadataReader metadata, GuidHandle handle)
			{
				this.metadata = metadata;
				this.handle = handle;
			}
		}
	}
}
