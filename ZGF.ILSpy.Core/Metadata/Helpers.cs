// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// Classifies a column for the DataGrid view (Phase 2+). Heap-offset cells render with a
	/// hex-padded width and resolve their string content via the relevant heap reader; token
	/// cells render as a hyperlink that navigates to the target table row. The text path
	/// (Phase 1) ignores everything except <c>Format</c>.
	/// </summary>
	internal enum ColumnKind
	{
		Other,
		HeapOffset,
		Token,
	}

	/// <summary>
	/// Annotates a row-shape property with rendering hints. <c>Format</c> is consumed by the
	/// text writer; <c>Kind</c> and <c>LinkToTable</c> drive the DataGrid column factory in
	/// Phase 2+ and are inert in Phase 1.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	internal sealed class ColumnInfoAttribute : Attribute
	{
		public string Format { get; }
		public ColumnKind Kind { get; set; }
		public bool LinkToTable { get; set; }

		public ColumnInfoAttribute(string format)
		{
			Format = format;
		}
	}

	/// <summary>
	/// One row in a PE-header / metadata-table dump. <c>Value</c> is rendered hex-formatted
	/// at <c>Size * 2</c> digits when numeric; <c>ValueTooltip</c> carries an optional flag-bit
	/// breakdown shown on hovering it. The text path ignores <c>ValueTooltip</c>.
	/// </summary>
	internal sealed class Entry
	{
		public string Member { get; }
		public int Offset { get; }
		public int Size { get; }
		public object? Value { get; }
		public string Meaning { get; }
		public IList<BitEntry>? ValueTooltip { get; }

		public Entry(int offset, object? value, int size, string member, string meaning, IList<BitEntry>? rowDetails = null)
		{
			Member = member;
			Offset = offset;
			Size = size;
			Value = value;
			Meaning = meaning;
			ValueTooltip = rowDetails;
		}
	}

	/// <summary>One bit (or bit-group) entry of a flags-Entry's breakdown.</summary>
	internal sealed class BitEntry
	{
		public bool Value { get; }
		public string Meaning { get; }

		public BitEntry(bool value, string meaning)
		{
			Value = value;
			Meaning = meaning;
		}
	}

	internal static class BlobReaderExtensions
	{
		/// <summary>
		/// Reads a null-terminated UTF-8 string and advances the reader past the terminator.
		/// Throws <see cref="BadImageFormatException"/> when no terminator remains, so callers
		/// parsing structured blobs can degrade to a hex dump on malformed input.
		/// </summary>
		public static string ReadUTF8StringNullTerminated(this ref BlobReader reader)
		{
			int length = reader.IndexOf(0);
			if (length < 0)
				throw new BadImageFormatException("Missing null terminator in blob.");
			string s = reader.ReadUTF8(length);
			reader.ReadByte();
			return s;
		}
	}

	/// <summary>
	/// Decoded text payload of a row (embedded source, source-link JSON, hex dump), tagged
	/// with the file extension (".cs", ".json") that selects the syntax highlighting for its
	/// display, or <see langword="null"/> for plain text.
	/// </summary>
	internal sealed record TextBlobDetail(string Text, string? HighlightExtension = null);
}
