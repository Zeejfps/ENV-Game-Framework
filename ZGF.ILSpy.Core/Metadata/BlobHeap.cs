// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of #Blob — variable-length byte payloads holding signatures, marshalling info,
	/// custom-attribute values, and similar binary data. Each entry is rendered as a hex
	/// dump; full structural decoding (e.g. parsed type signatures) is out of scope for
	/// the metadata viewer.
	/// </summary>
	internal sealed class BlobHeap : MetadataHeap<BlobHeap.BlobHeapEntry>
	{
		public BlobHeap(MetadataFile metadataFile)
			: base(HandleKind.Blob, metadataFile)
		{
		}

		protected override string HeapName => "Blob Heap";

		protected override void LoadEntries(MetadataReader metadata, List<BlobHeapEntry> list)
		{
			WalkHeap(list, MetadataTokens.BlobHandle(0),
				h => new BlobHeapEntry(metadata, h), metadata.GetNextHandle, h => h.IsNil);
		}

		public sealed class BlobHeapEntry
		{
			readonly MetadataReader metadata;
			readonly BlobHandle handle;

			[ColumnInfo("X8")]
			public int Offset => metadata.GetHeapOffset(handle);
			public int Length => metadata.GetBlobReader(handle).Length;
			public string Value => metadata.GetBlobReader(handle).ToHexString();

			public BlobHeapEntry(MetadataReader metadata, BlobHandle handle)
			{
				this.metadata = metadata;
				this.handle = handle;
			}
		}
	}
}
