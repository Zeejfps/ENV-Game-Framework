// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of #Strings — the heap that holds every UTF-8 type/member/namespace name
	/// referenced by the metadata tables. Lazy materialisation: handles are walked on
	/// demand via <see cref="MetadataReader.GetNextHandle(StringHandle)"/> the first time a
	/// caller asks for the count or a preview row.
	/// </summary>
	internal sealed class StringHeap : MetadataHeap<StringHeap.StringHeapEntry>
	{
		public StringHeap(MetadataFile metadataFile)
			: base(HandleKind.String, metadataFile)
		{
		}

		protected override string HeapName => "String Heap";

		protected override void LoadEntries(MetadataReader metadata, List<StringHeapEntry> list)
		{
			WalkHeap(list, MetadataTokens.StringHandle(0),
				h => new StringHeapEntry(metadata, h), metadata.GetNextHandle, h => h.IsNil);
		}

		public sealed class StringHeapEntry
		{
			readonly MetadataReader metadata;
			readonly StringHandle handle;

			[ColumnInfo("X8")]
			public int Offset => metadata.GetHeapOffset(handle);
			public int Length => metadata.GetString(handle).Length;
			public string Value => metadata.GetString(handle);

			public StringHeapEntry(MetadataReader metadata, StringHandle handle)
			{
				this.metadata = metadata;
				this.handle = handle;
			}
		}
	}
}
