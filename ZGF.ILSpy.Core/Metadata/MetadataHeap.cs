// Copyright (c) 2026 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// One of the four heaps (#Strings, #US, #GUID, #Blob), with the <see cref="HandleKind"/>
	/// that distinguishes them.
	/// </summary>
	internal abstract class MetadataHeap
	{
		protected readonly MetadataFile metadataFile;

		public HandleKind Kind { get; }

		protected MetadataHeap(HandleKind kind, MetadataFile metadataFile)
		{
			Kind = kind;
			this.metadataFile = metadataFile ?? throw new ArgumentNullException(nameof(metadataFile));
		}

		public abstract string Text { get; }

		/// <summary>The type of the rows <see cref="LoadRows"/> gives.</summary>
		public abstract Type EntryType { get; }

		/// <summary>Every row, in heap order, loaded once.</summary>
		public abstract IReadOnlyList<object> LoadRows();
	}

	/// <summary>
	/// Typed companion that owns the lazy row cache and the "{name} Heap (count)" header.
	/// Subclasses supply the heap's display name and materialise its rows, which is all that
	/// differs between the four heaps.
	/// </summary>
	internal abstract class MetadataHeap<TEntry> : MetadataHeap
		where TEntry : class
	{
		List<TEntry>? entries;

		protected MetadataHeap(HandleKind kind, MetadataFile metadataFile)
			: base(kind, metadataFile)
		{
		}

		public override Type EntryType => typeof(TEntry);

		public override IReadOnlyList<object> LoadRows() => EnsureEntries();

		/// <summary>The heap's display name, e.g. "Blob Heap".</summary>
		protected abstract string HeapName { get; }

		public override string Text => $"{HeapName} ({EnsureEntries().Count})";

		protected List<TEntry> EnsureEntries()
		{
			lock (this)
			{
				if (entries is { } cached)
					return cached;
				var list = new List<TEntry>();
				LoadEntries(metadataFile.Metadata, list);
				entries = list;
				return list;
			}
		}

		/// <summary>Materialises every heap row into <paramref name="list"/>.</summary>
		protected abstract void LoadEntries(MetadataReader metadata, List<TEntry> list);

		/// <summary>
		/// Shared walk for the offset-chained heaps (#Strings, #US, #Blob). Handle 0 is a
		/// real first entry (the heap's empty/zero slot) even though it is the nil handle,
		/// so each entry is added before <paramref name="getNextHandle"/> is consulted; the
		/// walk ends when the returned handle is nil.
		/// </summary>
		protected static void WalkHeap<THandle>(List<TEntry> list, THandle firstHandle,
			Func<THandle, TEntry> createEntry, Func<THandle, THandle> getNextHandle,
			Predicate<THandle> isNil)
			where THandle : struct
		{
			var handle = firstHandle;
			do
			{
				list.Add(createEntry(handle));
				handle = getNextHandle(handle);
			} while (!isNil(handle));
		}
	}
}
