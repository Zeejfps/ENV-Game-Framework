// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of #US — the UTF-16 heap holding every literal string emitted by an <c>ldstr</c>
	/// instruction. Distinct from #Strings (which carries identifier names); user strings
	/// are referenced by metadata tokens, not by member-name resolution.
	/// </summary>
	internal sealed class UserStringHeap : MetadataHeap<UserStringHeap.UserStringHeapEntry>
	{
		public UserStringHeap(MetadataFile metadataFile)
			: base(HandleKind.UserString, metadataFile)
		{
		}

		protected override string HeapName => "UserString Heap";

		protected override void LoadEntries(MetadataReader metadata, List<UserStringHeapEntry> list)
		{
			WalkHeap(list, MetadataTokens.UserStringHandle(0),
				h => new UserStringHeapEntry(metadata, h), metadata.GetNextHandle, h => h.IsNil);
		}

		public sealed class UserStringHeapEntry
		{
			readonly MetadataReader metadata;
			readonly UserStringHandle handle;

			[ColumnInfo("X8")]
			public int Offset => metadata.GetHeapOffset(handle);
			public int Length => metadata.GetUserString(handle).Length;
			public string Value => metadata.GetUserString(handle);

			public UserStringHeapEntry(MetadataReader metadata, UserStringHandle handle)
			{
				this.metadata = metadata;
				this.handle = handle;
			}
		}
	}
}
