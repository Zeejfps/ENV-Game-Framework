// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>The rows of the PE header views: DOS, COFF and Optional headers, the data
	/// directories and the debug directory.</summary>
	internal static class PEHeaderEntries
	{
		/// <summary>The 64-byte legacy DOS header sitting at offset 0 of every PE file.</summary>
		public static IReadOnlyList<Entry> Dos(PEFile module)
		{
			var reader = module.Reader.GetEntireImage().GetReader(0, 64);
			var entries = new List<Entry>(31) {
				new(reader.Offset, reader.ReadUInt16(), 2, "e_magic", "Magic Number (MZ)"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_cblp", "Bytes on last page of file"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_cp", "Pages in file"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_crlc", "Relocations"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_cparhdr", "Size of header in paragraphs"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_minalloc", "Minimum extra paragraphs needed"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_maxalloc", "Maximum extra paragraphs needed"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_ss", "Initial (relative) SS value"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_sp", "Initial SP value"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_csum", "Checksum"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_ip", "Initial IP value"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_cs", "Initial (relative) CS value"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_lfarlc", "File address of relocation table"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_ovno", "Overlay number"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res[0]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res[1]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res[2]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res[3]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_oemid", "OEM identifier (for e_oeminfo)"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_oeminfo", "OEM information; e_oemid specific"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[0]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[1]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[2]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[3]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[4]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[5]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[6]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[7]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[8]", "Reserved words"),
				new(reader.Offset, reader.ReadUInt16(), 2, "e_res2[9]", "Reserved words"),
				new(reader.Offset, reader.ReadInt32(), 4, "e_lfanew", "File address of new exe header"),
			};
			return entries;
		}

		/// <summary>COFF (Common Object File Format) header — 20 bytes describing target machine, section
		/// count, link timestamp, and file-level characteristics. The Characteristics field is a
		/// 16-bit flags word; each individual bit is broken out as a row-detail entry.</summary>
		public static IReadOnlyList<Entry> Coff(PEFile module)
		{
			var headers = module.Reader.PEHeaders;
			var header = headers.CoffHeader;
			var linkerDateTime = DateTimeOffset.FromUnixTimeSeconds(unchecked((uint)header.TimeDateStamp)).DateTime;
			int characteristics = (int)header.Characteristics;
			return new List<Entry> {
				new(headers.CoffHeaderStartOffset, (int)header.Machine, 2, "Machine", header.Machine.ToString()),
				new(headers.CoffHeaderStartOffset + 2, (int)header.NumberOfSections, 2, "Number of Sections", "Number of sections; indicates size of the Section Table, which immediately follows the headers."),
				new(headers.CoffHeaderStartOffset + 4, header.TimeDateStamp, 4, "Time/Date Stamp", $"{linkerDateTime} (UTC) / {linkerDateTime.ToLocalTime()} - Time and date the file was created in seconds since January 1st 1970 00:00:00 or 0. Note that for deterministic builds this value is meaningless."),
				new(headers.CoffHeaderStartOffset + 8, header.PointerToSymbolTable, 4, "Pointer to Symbol Table", "Always 0 in .NET executables."),
				new(headers.CoffHeaderStartOffset + 12, header.NumberOfSymbols, 4, "Number of Symbols", "Always 0 in .NET executables."),
				new(headers.CoffHeaderStartOffset + 16, (int)header.SizeOfOptionalHeader, 2, "Optional Header Size", "Size of the optional header."),
				new(headers.CoffHeaderStartOffset + 18, characteristics, 2, "Characteristics", "Flags indicating attributes of the file.", new BitEntry[] {
					new((characteristics & 0x0001) != 0, "<0001> Relocation info stripped from file"),
					new((characteristics & 0x0002) != 0, "<0002> File is executable"),
					new((characteristics & 0x0004) != 0, "<0004> Line numbers stripped from file"),
					new((characteristics & 0x0008) != 0, "<0008> Local symbols stripped from file"),
					new((characteristics & 0x0010) != 0, "<0010> Aggressively trim working set"),
					new((characteristics & 0x0020) != 0, "<0020> Large address aware"),
					new((characteristics & 0x0040) != 0, "<0040> Reserved"),
					new((characteristics & 0x0080) != 0, "<0080> Bytes of machine words are reversed (Low)"),
					new((characteristics & 0x0100) != 0, "<0100> 32-bit word machine"),
					new((characteristics & 0x0200) != 0, "<0200> Debugging info stripped from file in .DBG file"),
					new((characteristics & 0x0400) != 0, "<0400> If image is on removable media, copy and run from the swap file"),
					new((characteristics & 0x0800) != 0, "<0800> If image is on Net, copy and run from the swap file"),
					new((characteristics & 0x1000) != 0, "<1000> System"),
					new((characteristics & 0x2000) != 0, "<2000> DLL"),
					new((characteristics & 0x4000) != 0, "<4000> File should only be run on a UP machine"),
					new((characteristics & 0x8000) != 0, "<8000> Bytes of machine words are reversed (High)"),
				}),
			};
		}

		/// <summary>PE Optional Header — image base, sizes, alignments, subsystem. Field widths flex
		/// between PE32 and PE32+ (Image Base / Stack &amp; Heap reserves go from 4 → 8 bytes,
		/// Base Of Data drops out on PE32+).</summary>
		public static IReadOnlyList<Entry> Optional(PEFile module)
		{
			var headers = module.Reader.PEHeaders;
			var reader = module.Reader.GetEntireImage().GetReader(headers.PEHeaderStartOffset, 128);
			var header = headers.PEHeader!;
			bool isPE32Plus = header.Magic == PEMagic.PE32Plus;

			var entries = new List<Entry>();
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt16(), 2, "Magic", header.Magic.ToString()));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadByte(), 1, "Major Linker Version", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadByte(), 1, "Minor Linker Version", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "Code Size", "Size of the code (text) section, or the sum of all code sections if there are multiple sections."));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "Initialized Data Size", "Size of the initialized data section, or the sum of all initialized data sections if there are multiple data sections."));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "Uninitialized Data Size", "Size of the uninitialized data section, or the sum of all uninitialized data sections if there are multiple uninitialized data sections."));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "Entry Point RVA", "RVA of entry point, needs to point to bytes 0xFF 0x25 followed by the RVA in a section marked execute / read for EXEs or 0 for DLLs"));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "Base Of Code", "RVA of the code section."));
			entries.Add(new Entry(isPE32Plus ? 0 : headers.PEHeaderStartOffset + reader.Offset, isPE32Plus ? 0UL : reader.ReadUInt32(), isPE32Plus ? 0 : 4, "Base Of Data", "PE32 only (not present in PE32Plus): RVA of the data section, relative to the Image Base."));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, isPE32Plus ? reader.ReadUInt64() : reader.ReadUInt32(), isPE32Plus ? 8 : 4, "Image Base", "Shall be a multiple of 0x10000."));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "Section Alignment", "Shall be greater than File Alignment."));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "File Alignment", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt16(), 2, "Major OS Version", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt16(), 2, "Minor OS Version", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt16(), 2, "Major Image Version", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt16(), 2, "Minor Image Version", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt16(), 2, "Major Subsystem Version", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt16(), 2, "Minor Subsystem Version", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt32(), 4, "Win32VersionValue", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "Image Size", "Size, in bytes, of image, including all headers and padding; shall be a multiple of Section Alignment."));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "Header Size", "Combined size of MS-DOS Header, PE Header, PE Optional Header and padding; shall be a multiple of the file alignment."));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "File Checksum", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt16(), 2, "Subsystem", header.Subsystem.ToString()));
			ushort dllCharacteristics = reader.ReadUInt16();
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset - 2, dllCharacteristics, 2, "DLL Characteristics", header.DllCharacteristics.ToString(), new BitEntry[] {
				new((dllCharacteristics & 0x0001) != 0, "<0001> Process Init (Reserved)"),
				new((dllCharacteristics & 0x0002) != 0, "<0002> Process Term (Reserved)"),
				new((dllCharacteristics & 0x0004) != 0, "<0004> Thread Init (Reserved)"),
				new((dllCharacteristics & 0x0008) != 0, "<0008> Thread Term (Reserved)"),
				new((dllCharacteristics & 0x0010) != 0, "<0010> Unused"),
				new((dllCharacteristics & 0x0020) != 0, "<0020> Image can handle a high entropy 64-bit virtual address space (ASLR)"),
				new((dllCharacteristics & 0x0040) != 0, "<0040> DLL can be relocated at load time"),
				new((dllCharacteristics & 0x0080) != 0, "<0080> Code integrity checks are enforced"),
				new((dllCharacteristics & 0x0100) != 0, "<0100> Image is NX compatible"),
				new((dllCharacteristics & 0x0200) != 0, "<0200> Isolation aware, but do not isolate the image"),
				new((dllCharacteristics & 0x0400) != 0, "<0400> Does not use structured exception handling (SEH)"),
				new((dllCharacteristics & 0x0800) != 0, "<0800> Do not bind the image"),
				new((dllCharacteristics & 0x1000) != 0, "<1000> Image must execute in an AppContainer"),
				new((dllCharacteristics & 0x2000) != 0, "<2000> Driver is a WDM Driver"),
				new((dllCharacteristics & 0x4000) != 0, "<4000> Image supports Control Flow Guard"),
				new((dllCharacteristics & 0x8000) != 0, "<8000> Image is Terminal Server aware"),
			}));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, isPE32Plus ? reader.ReadUInt64() : reader.ReadUInt32(), isPE32Plus ? 8 : 4, "Stack Reserve Size", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, isPE32Plus ? reader.ReadUInt64() : reader.ReadUInt32(), isPE32Plus ? 8 : 4, "Stack Commit Size", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, isPE32Plus ? reader.ReadUInt64() : reader.ReadUInt32(), isPE32Plus ? 8 : 4, "Heap Reserve Size", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, isPE32Plus ? reader.ReadUInt64() : reader.ReadUInt32(), isPE32Plus ? 8 : 4, "Heap Commit Size", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadUInt32(), 4, "Loader Flags", ""));
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset, reader.ReadInt32(), 4, "Number of Data Directories", ""));
			return entries;
		}

		/// <summary>
		/// 15-row table of (RVA, Size, Containing-Section) triples that lives at the tail of the
		/// PE Optional Header. Each entry points at one well-known PE region (Export Table,
		/// Import Table, CLI Header, …).
		/// </summary>
		public static IReadOnlyList<DataDirectoryEntry> DataDirectories(PEFile module)
		{
			var headers = module.Reader.PEHeaders;
			var header = headers.PEHeader!;
			return new List<DataDirectoryEntry> {
				new(headers, "Export Table", header.ExportTableDirectory),
				new(headers, "Import Table", header.ImportTableDirectory),
				new(headers, "Resource Table", header.ResourceTableDirectory),
				new(headers, "Exception Table", header.ExceptionTableDirectory),
				new(headers, "Certificate Table", header.CertificateTableDirectory),
				new(headers, "Base Relocation Table", header.BaseRelocationTableDirectory),
				new(headers, "Debug Table", header.DebugTableDirectory),
				new(headers, "Copyright Table", header.CopyrightTableDirectory),
				new(headers, "Global Pointer Table", header.GlobalPointerTableDirectory),
				new(headers, "Thread Local Storage Table", header.ThreadLocalStorageTableDirectory),
				new(headers, "Load Config", header.LoadConfigTableDirectory),
				new(headers, "Bound Import", header.BoundImportTableDirectory),
				new(headers, "Import Address Table", header.ImportAddressTableDirectory),
				new(headers, "Delay Import Descriptor", header.DelayImportTableDirectory),
				new(headers, "CLI Header", header.CorHeaderTableDirectory),
			};
		}

		/// <summary>
		/// Each entry of the PE debug directory: timestamp, version, type, and the size /
		/// addresses of the raw debug payload.
		/// </summary>
		public static IReadOnlyList<DebugDirectoryEntryView> DebugDirectory(PEFile module)
		{
			var entries = new List<DebugDirectoryEntryView>();
			foreach (var entry in module.Reader.ReadDebugDirectory())
			{
				int dataOffset = module.Reader.IsLoadedImage ? entry.DataRelativeVirtualAddress : entry.DataPointer;
				var data = module.Reader.GetEntireImage().GetContent(dataOffset, entry.DataSize);
				entries.Add(new DebugDirectoryEntryView(entry, data.ToHexString(data.Length)));
			}
			return entries;
		}

		public sealed class DataDirectoryEntry
		{
			public string Name { get; }
			[ColumnInfo("X8")]
			public int RVA { get; }
			[ColumnInfo("X8")]
			public int Size { get; }
			public string Section { get; }

			public DataDirectoryEntry(PEHeaders headers, string name, DirectoryEntry entry)
			{
				Name = name;
				RVA = entry.RelativeVirtualAddress;
				Size = entry.Size;
				int sectionIndex = headers.GetContainingSectionIndex(entry.RelativeVirtualAddress);
				Section = sectionIndex >= 0 ? headers.SectionHeaders[sectionIndex].Name : "";
			}
		}

		public sealed class DebugDirectoryEntryView
		{
			public uint Timestamp { get; }
			public ushort MajorVersion { get; }
			public ushort MinorVersion { get; }
			public string Type { get; }
			public int SizeOfRawData { get; }
			[ColumnInfo("X8")]
			public int AddressOfRawData { get; }
			[ColumnInfo("X8")]
			public int PointerToRawData { get; }
			public string RawData { get; }

			public DebugDirectoryEntryView(DebugDirectoryEntry entry, string data)
			{
				Timestamp = entry.Stamp;
				MajorVersion = entry.MajorVersion;
				MinorVersion = entry.MinorVersion;
				Type = entry.Type.ToString();
				SizeOfRawData = entry.DataSize;
				AddressOfRawData = entry.DataRelativeVirtualAddress;
				PointerToRawData = entry.DataPointer;
				RawData = data;
			}
		}
	}
}
