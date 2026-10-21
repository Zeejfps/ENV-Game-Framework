using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata;

/// <summary>A metadata node shown as comment lines rather than a grid.</summary>
internal abstract class MetadataTextNode(LoadedAssembly assembly, BrowseNode parent, NodeKind kind, string text)
    : BrowseNode(assembly, parent, kind, text)
{
    internal abstract IEnumerable<string> Lines();
}

/// <summary>
/// ILSpy's "Metadata" folder: the raw CLI metadata of <see cref="File"/> — for a PE file its
/// headers and debug directory, then the tables and the four heaps.
/// </summary>
internal sealed class MetadataFolderNode(LoadedAssembly assembly, BrowseNode parent, MetadataFile file, string title)
    : MetadataTextNode(assembly, parent, NodeKind.Metadata, title)
{
    public MetadataFile File { get; } = file;

    private protected override bool MayHaveChildren => true;

    /// <summary>The node listing the rows of <paramref name="table"/>, or null when it has none.</summary>
    public MetadataGridNode? FindTable(TableIndex table) =>
        Children.OfType<MetadataTablesNode>().FirstOrDefault()?.Children.OfType<MetadataGridNode>().FirstOrDefault(n => n.Table == table);

    internal override IEnumerable<string> Lines()
    {
        var metadata = File.Metadata;
        yield return Text;
        yield return "MetadataKind: " + metadata.MetadataKind;
        yield return "MetadataVersion: " + metadata.MetadataVersion;
        if (metadata.DebugMetadataHeader is { } header)
        {
            yield return "";
            yield return "Header:";
            yield return "Id: " + Convert.ToHexStringLower(header.Id.AsSpan());
            yield return "EntryPoint: " + MetadataTokens.GetToken(header.EntryPoint).ToString("X8");
        }
        yield return "";
        yield return "Tables:";
        foreach (var line in MetadataTablesNode.Counts(metadata))
            yield return line;
    }

    private protected override BrowseNode[] CreateChildren()
    {
        var result = new List<BrowseNode>();
        if (File is PEFile pe)
        {
            result.Add(new MetadataGridNode(Assembly, this, NodeKind.MetadataHeader, "DOS Header", File, typeof(Entry), () => PEHeaderEntries.Dos(pe)));
            result.Add(new MetadataGridNode(Assembly, this, NodeKind.MetadataHeader, "COFF Header", File, typeof(Entry), () => PEHeaderEntries.Coff(pe)));
            result.Add(new MetadataGridNode(Assembly, this, NodeKind.MetadataHeader, "Optional Header", File, typeof(Entry), () => PEHeaderEntries.Optional(pe)));
            result.Add(new MetadataGridNode(Assembly, this, NodeKind.MetadataDirectory, "Data Directories", File,
                typeof(PEHeaderEntries.DataDirectoryEntry), () => PEHeaderEntries.DataDirectories(pe)));
            result.Add(new DebugDirectoryNode(Assembly, this, pe));
        }
        result.Add(new MetadataTablesNode(Assembly, this, File));
        foreach (MetadataHeap heap in (ReadOnlySpan<MetadataHeap>)[new StringHeap(File), new UserStringHeap(File), new GuidHeap(File), new BlobHeap(File)])
            result.Add(new MetadataGridNode(Assembly, this, NodeKind.MetadataHeap, heap.Text, File, heap.EntryType, heap.LoadRows));
        return result.ToArray();
    }

    /// <summary>The portable PDB that goes with <paramref name="file"/> as ILSpy shows it beside
    /// the "Metadata" folder: the one embedded in it, else the one next to it whose id matches its
    /// CodeView entry. Null when there is none, or it cannot be read.</summary>
    public static (MetadataFile File, string Title)? DebugMetadataOf(PEFile file)
    {
        try
        {
            var reader = file.Reader;
            var entries = reader.ReadDebugDirectory();
            foreach (var entry in entries)
            {
                if (entry.Type != DebugDirectoryEntryType.EmbeddedPortablePdb) continue;
                var provider = reader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
                return (new MetadataFile(MetadataFile.MetadataFileKind.ProgramDebugDatabase, file.FileName, provider, isEmbedded: true),
                    "Debug Metadata (Embedded)");
            }
            foreach (var entry in entries)
            {
                if (entry.Type != DebugDirectoryEntryType.CodeView) continue;
                var codeView = reader.ReadCodeViewDebugDirectoryData(entry);
                var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file.FileName) ?? "", System.IO.Path.GetFileName(codeView.Path));
                if (!System.IO.File.Exists(path))
                    path = System.IO.Path.ChangeExtension(file.FileName, ".pdb");
                if (!System.IO.File.Exists(path)) continue;
                var provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(System.IO.File.ReadAllBytes(path)));
                var id = new BlobContentId(provider.GetMetadataReader().DebugMetadataHeader!.Id);
                if (id.Guid != codeView.Guid || id.Stamp != entry.Stamp) continue;
                return (new MetadataFile(MetadataFile.MetadataFileKind.ProgramDebugDatabase, path, provider), "Debug Metadata (From portable PDB)");
            }
        }
        catch (Exception e) when (e is BadImageFormatException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
        }
        return null;
    }
}

/// <summary>ILSpy's "Tables" folder: one node per table with rows.</summary>
internal sealed class MetadataTablesNode(LoadedAssembly assembly, BrowseNode parent, MetadataFile file)
    : MetadataTextNode(assembly, parent, NodeKind.MetadataTables, "Tables")
{
    private protected override bool MayHaveChildren => true;

    internal static IEnumerable<string> Counts(MetadataReader metadata)
    {
        foreach (var table in Enum.GetValues<TableIndex>())
        {
            var count = metadata.GetTableRowCount(table);
            if (count > 0)
                yield return $"{(byte)table:X2} {table}: {count} rows";
        }
    }

    internal override IEnumerable<string> Lines() => Counts(file.Metadata).Prepend("Tables");

    private protected override BrowseNode[] CreateChildren()
    {
        var metadata = file.Metadata;
        var result = new List<BrowseNode>();
        foreach (var index in Enum.GetValues<TableIndex>())
        {
            if (metadata.GetTableRowCount(index) == 0) continue;
            if (MetadataTables.Create(index, file) is { } table)
                result.Add(new MetadataGridNode(Assembly, this, NodeKind.MetadataTable, table.Text, file, table.EntryType, table.LoadRows) { Table = index });
            else
                result.Add(new UnsupportedTableNode(Assembly, this, index, metadata.GetTableRowCount(index)));
        }
        return result.ToArray();
    }
}

/// <summary>A table whose rows ILSpy does not list: only how many there are.</summary>
internal sealed class UnsupportedTableNode(LoadedAssembly assembly, BrowseNode parent, TableIndex table, int rows)
    : MetadataTextNode(assembly, parent, NodeKind.MetadataTable, $"{(byte)table:X2} {table} ({rows})")
{
    internal override IEnumerable<string> Lines() => [$"{table}: {rows} rows"];
}

/// <summary>A metadata node shown as a grid: a header, a directory, a table or a heap.</summary>
internal class MetadataGridNode : BrowseNode
{
    private readonly Type _entryType;
    private readonly Func<IReadOnlyList<object>> _rows;
    private readonly Lock _gate = new();
    private MetadataGrid? _grid;

    public MetadataGridNode(LoadedAssembly assembly, BrowseNode parent, NodeKind kind, string text, MetadataFile file, Type entryType,
        Func<IReadOnlyList<object>> rows)
        : base(assembly, parent, kind, text)
    {
        File = file;
        _entryType = entryType;
        _rows = rows;
    }

    public MetadataFile File { get; }

    /// <summary>The table this lists, for a table node.</summary>
    public TableIndex? Table { get; init; }

    /// <summary>The "Metadata" folder this belongs to.</summary>
    public MetadataFolderNode? Folder
    {
        get
        {
            for (var node = Parent; node != null; node = node.Parent)
            {
                if (node is MetadataFolderNode folder) return folder;
            }
            return null;
        }
    }

    public MetadataGrid Grid
    {
        get
        {
            lock (_gate)
                return _grid ??= new MetadataGrid(this, _entryType, _rows());
        }
    }
}

/// <summary>ILSpy's "Debug Directory": its entries as a grid, and each as a node of its own.</summary>
internal sealed class DebugDirectoryNode(LoadedAssembly assembly, BrowseNode parent, PEFile file)
    : MetadataGridNode(assembly, parent, NodeKind.MetadataDirectory, "Debug Directory", file, typeof(PEHeaderEntries.DebugDirectoryEntryView),
        () => PEHeaderEntries.DebugDirectory(file))
{
    private protected override bool MayHaveChildren => true;

    private protected override BrowseNode[] CreateChildren()
    {
        var reader = file.Reader;
        var result = new List<BrowseNode>();
        foreach (var entry in reader.ReadDebugDirectory())
        {
            try
            {
                switch (entry.Type)
                {
                    case DebugDirectoryEntryType.CodeView:
                        var codeView = reader.ReadCodeViewDebugDirectoryData(entry);
                        result.Add(new DebugEntryNode(Assembly, this, file, entry, [$"GUID: {codeView.Guid}", $"Age: {codeView.Age}", $"Path: {codeView.Path}"]));
                        continue;
                    case DebugDirectoryEntryType.PdbChecksum:
                        var checksum = reader.ReadPdbChecksumDebugDirectoryData(entry);
                        result.Add(new DebugEntryNode(Assembly, this, file, entry,
                            [$"AlgorithmName: {checksum.AlgorithmName}", $"Checksum: {Convert.ToHexStringLower(checksum.Checksum.AsSpan())}"]));
                        continue;
                    case DebugDirectoryEntryType.EmbeddedPortablePdb:
                        var provider = reader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
                        var pdb = new MetadataFile(MetadataFile.MetadataFileKind.ProgramDebugDatabase, file.FileName, provider, isEmbedded: true);
                        result.Add(new MetadataFolderNode(Assembly, this, pdb, "Debug Metadata (Embedded)"));
                        continue;
                }
            }
            catch (BadImageFormatException)
            {
                if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb) continue;
            }
            result.Add(new DebugEntryNode(Assembly, this, file, entry, null));
        }
        return result.ToArray();
    }
}

/// <summary>One entry of the debug directory: what ILSpy decodes of it, else its raw bytes.</summary>
internal sealed class DebugEntryNode(LoadedAssembly assembly, BrowseNode parent, PEFile file, DebugDirectoryEntry entry, string[]? decoded)
    : MetadataTextNode(assembly, parent, NodeKind.MetadataTable, entry.Type.ToString())
{
    internal override IEnumerable<string> Lines()
    {
        yield return Text;
        if (decoded != null)
        {
            foreach (var line in decoded)
                yield return line;
            yield break;
        }
        if (entry.DataSize > 0)
        {
            yield return $"Raw Data ({entry.DataSize}):";
            var offset = file.Reader.IsLoadedImage ? entry.DataRelativeVirtualAddress : entry.DataPointer;
            var data = file.Reader.GetEntireImage().GetContent(offset, entry.DataSize);
            yield return Convert.ToHexStringLower(data.AsSpan());
        }
        else
        {
            yield return "(no data)";
        }
    }
}
