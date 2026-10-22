// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>The grids of the PE header views: DOS, COFF and Optional headers, the data
	/// directories and the debug directory.</summary>
	internal static class PEHeaderEntries
	{
		/// <summary>The 64-byte legacy DOS header sitting at offset 0 of every PE file.</summary>
		public static GridSchema Dos(PEFile module) => Header("DOS Header", module, () => DosEntries(module));

		/// <summary>COFF (Common Object File Format) header — 20 bytes describing target machine, section
		/// count, link timestamp, and file-level characteristics. The Characteristics field is a
		/// 16-bit flags word whose bits its value's tooltip breaks out.</summary>
		public static GridSchema Coff(PEFile module) => Header("COFF Header", module, () => CoffEntries(module));

		/// <summary>PE Optional Header — image base, sizes, alignments, subsystem. Field widths flex
		/// between PE32 and PE32+ (Image Base / Stack &amp; Heap reserves go from 4 → 8 bytes,
		/// Base Of Data drops out on PE32+).</summary>
		public static GridSchema Optional(PEFile module) => Header("Optional Header", module, () => OptionalEntries(module));

		static GridSchema<Entry> Header(string key, PEFile module, Func<IEnumerable<Entry>> rows) =>
			new GridSchema<Entry>(key, module, rows)
				.Value("Member", e => e.Member)
				.Value("Offset", e => e.Offset)
				.Value("Size", e => e.Size)
				.Value("Value", e => e.Value, tooltip: e => e.Flags?.Explain(Convert.ToUInt32(e.Value)))
				.Value("Meaning", e => e.Meaning);

		static readonly FlagsSchema CharacteristicsBits = Bits(typeof(Characteristics), [
			"Relocation info stripped from file",
			"File is executable",
			"Line numbers stripped from file",
			"Local symbols stripped from file",
			"Aggressively trim working set",
			"Large address aware",
			"Reserved",
			"Bytes of machine words are reversed (Low)",
			"32-bit word machine",
			"Debugging info stripped from file in .DBG file",
			"If image is on removable media, copy and run from the swap file",
			"If image is on Net, copy and run from the swap file",
			"System",
			"DLL",
			"File should only be run on a UP machine",
			"Bytes of machine words are reversed (High)",
		]);

		static readonly FlagsSchema DllCharacteristicsBits = Bits(typeof(DllCharacteristics), [
			"Process Init (Reserved)",
			"Process Term (Reserved)",
			"Thread Init (Reserved)",
			"Thread Term (Reserved)",
			"Unused",
			"Image can handle a high entropy 64-bit virtual address space (ASLR)",
			"DLL can be relocated at load time",
			"Code integrity checks are enforced",
			"Image is NX compatible",
			"Isolation aware, but do not isolate the image",
			"Does not use structured exception handling (SEH)",
			"Do not bind the image",
			"Image must execute in an AppContainer",
			"Driver is a WDM Driver",
			"Image supports Control Flow Guard",
			"Image is Terminal Server aware",
		]);

		static FlagsSchema Bits(Type type, string[] meanings) =>
			new(type, [], meanings.Select(static (m, i) => new IndependentFlag(m, 1u << i)).ToArray());

		static List<Entry> DosEntries(PEFile module)
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

		static List<Entry> CoffEntries(PEFile module)
		{
			var headers = module.Reader.PEHeaders;
			var header = headers.CoffHeader;
			var linkerDateTime = DateTimeOffset.FromUnixTimeSeconds(unchecked((uint)header.TimeDateStamp)).DateTime;
			return new List<Entry> {
				new(headers.CoffHeaderStartOffset, (int)header.Machine, 2, "Machine", header.Machine.ToString()),
				new(headers.CoffHeaderStartOffset + 2, (int)header.NumberOfSections, 2, "Number of Sections", "Number of sections; indicates size of the Section Table, which immediately follows the headers."),
				new(headers.CoffHeaderStartOffset + 4, header.TimeDateStamp, 4, "Time/Date Stamp", $"{linkerDateTime} (UTC) / {linkerDateTime.ToLocalTime()} - Time and date the file was created in seconds since January 1st 1970 00:00:00 or 0. Note that for deterministic builds this value is meaningless."),
				new(headers.CoffHeaderStartOffset + 8, header.PointerToSymbolTable, 4, "Pointer to Symbol Table", "Always 0 in .NET executables."),
				new(headers.CoffHeaderStartOffset + 12, header.NumberOfSymbols, 4, "Number of Symbols", "Always 0 in .NET executables."),
				new(headers.CoffHeaderStartOffset + 16, (int)header.SizeOfOptionalHeader, 2, "Optional Header Size", "Size of the optional header."),
				new(headers.CoffHeaderStartOffset + 18, (int)header.Characteristics, 2, "Characteristics", "Flags indicating attributes of the file.", CharacteristicsBits),
			};
		}

		static List<Entry> OptionalEntries(PEFile module)
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
			entries.Add(new Entry(headers.PEHeaderStartOffset + reader.Offset - 2, dllCharacteristics, 2, "DLL Characteristics", header.DllCharacteristics.ToString(), DllCharacteristicsBits));
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
		public static GridSchema DataDirectories(PEFile module)
		{
			var headers = module.Reader.PEHeaders;
			var header = headers.PEHeader!;
			(string Name, DirectoryEntry Entry)[] rows = [
				("Export Table", header.ExportTableDirectory),
				("Import Table", header.ImportTableDirectory),
				("Resource Table", header.ResourceTableDirectory),
				("Exception Table", header.ExceptionTableDirectory),
				("Certificate Table", header.CertificateTableDirectory),
				("Base Relocation Table", header.BaseRelocationTableDirectory),
				("Debug Table", header.DebugTableDirectory),
				("Copyright Table", header.CopyrightTableDirectory),
				("Global Pointer Table", header.GlobalPointerTableDirectory),
				("Thread Local Storage Table", header.ThreadLocalStorageTableDirectory),
				("Load Config", header.LoadConfigTableDirectory),
				("Bound Import", header.BoundImportTableDirectory),
				("Import Address Table", header.ImportAddressTableDirectory),
				("Delay Import Descriptor", header.DelayImportTableDirectory),
				("CLI Header", header.CorHeaderTableDirectory),
			];
			return new GridSchema<(string Name, DirectoryEntry Entry)>("Data Directories", module, () => rows)
				.Value("Name", r => r.Name)
				.Value("RVA", r => r.Entry.RelativeVirtualAddress, "X8")
				.Value("Size", r => r.Entry.Size, "X8")
				.Value("Section", r => headers.GetContainingSectionIndex(r.Entry.RelativeVirtualAddress) is >= 0 and var index ? headers.SectionHeaders[index].Name : "");
		}

		/// <summary>
		/// Each entry of the PE debug directory: timestamp, version, type, and the size /
		/// addresses of the raw debug payload.
		/// </summary>
		public static GridSchema DebugDirectory(PEFile module)
		{
			var reader = module.Reader;
			return new GridSchema<DebugDirectoryEntry>("Debug Directory", module, () => reader.ReadDebugDirectory())
				.Value("Timestamp", e => e.Stamp)
				.Value("MajorVersion", e => e.MajorVersion)
				.Value("MinorVersion", e => e.MinorVersion)
				.Value("Type", e => e.Type.ToString())
				.Value("SizeOfRawData", e => e.DataSize)
				.Value("AddressOfRawData", e => e.DataRelativeVirtualAddress, "X8")
				.Value("PointerToRawData", e => e.DataPointer, "X8")
				.Value("RawData", e =>
				{
					var data = reader.GetEntireImage().GetContent(reader.IsLoadedImage ? e.DataRelativeVirtualAddress : e.DataPointer, e.DataSize);
					return data.ToHexString(data.Length);
				});
		}
	}

	/// <summary>One row of a PE header: <see cref="Value"/> is <see cref="Size"/> bytes at
	/// <see cref="Offset"/>, and <see cref="Flags"/> breaks a flags value down.</summary>
	internal sealed record Entry(int Offset, object? Value, int Size, string Member, string Meaning, FlagsSchema? Flags = null);
}
