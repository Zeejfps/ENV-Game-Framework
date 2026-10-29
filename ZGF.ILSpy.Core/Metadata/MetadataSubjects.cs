using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata;

/// <summary>The raw CLI metadata of a file of <see cref="Module"/>: its own, or its debug metadata, under
/// a folder titled <see cref="Title"/>.</summary>
public sealed class MetadataFolderInfo
{
    internal MetadataFolderInfo(LoadedModule module, MetadataFile file, string title)
    {
        Module = module;
        File = file;
        Title = title;
    }

    public LoadedModule Module { get; }

    public MetadataFile File { get; }

    public string Title { get; }

    /// <summary>The folder as comment lines: its title, kind and version, a debug header's id and entry
    /// point, and how many rows each table has.</summary>
    internal IEnumerable<string> Lines()
    {
        var metadata = File.Metadata;
        yield return Title;
        yield return "MetadataKind: " + metadata.MetadataKind;
        yield return "MetadataVersion: " + metadata.MetadataVersion;
        var header = metadata.DebugMetadataHeader;
        if (header != null)
        {
            yield return "";
            yield return "Header:";
            yield return "Id: " + Convert.ToHexStringLower(header.Id.AsSpan());
            yield return "EntryPoint: " + MetadataTokens.GetToken(header.EntryPoint).ToString("X8");
        }
        yield return "";
        yield return "Tables:";
        foreach (var line in CountRows())
            yield return line;
    }

    /// <summary>The Tables folder as comment lines: how many rows each table has.</summary>
    internal IEnumerable<string> TablesLines() => CountRows().Prepend("Tables");

    private IEnumerable<string> CountRows()
    {
        var metadata = File.Metadata;
        foreach (var table in Enum.GetValues<TableIndex>())
        {
            var count = metadata.GetTableRowCount(table);
            if (count > 0)
                yield return $"{(byte)table:X2} {table}: {count} rows";
        }
    }
}

/// <summary>What a grid of metadata lists.</summary>
public enum MetadataGridRole
{
    Header,
    Directory,
    Table,
    Heap,
}

/// <summary>A part of a metadata folder: a grid of a header, directory, table or heap; the debug
/// directory, a grid that also lists its entries; a table shown only by how many rows it has; or a debug
/// directory entry, decoded or as its raw bytes.</summary>
internal abstract record MetadataPart
{
    private MetadataPart()
    {
    }

    public sealed record Grid(MetadataGridRole Role, GridSchema Schema, Lazy<MetadataGrid> View) : MetadataPart;

    public sealed record DebugDirectory(PEFile File, Lazy<MetadataGrid> View) : MetadataPart;

    public sealed record RowCount(TableIndex Table, int Rows) : MetadataPart;

    public sealed record DecodedDebugEntry(IReadOnlyList<string> Lines) : MetadataPart;

    public sealed record RawDebugEntry(PEFile File, DebugDirectoryEntry Entry) : MetadataPart;
}

/// <summary>A part of the metadata of a file of <see cref="Module"/>, titled <see cref="Title"/>: a grid
/// of a header, directory, table or heap, the debug directory, a table shown only by its row count, or
/// an entry of the debug directory.</summary>
public sealed class MetadataTableInfo
{
    private MetadataTableInfo(LoadedModule module, MetadataFile file, string title, MetadataPart part)
    {
        Module = module;
        File = file;
        Title = title;
        Part = part;
    }

    public LoadedModule Module { get; }

    public MetadataFile File { get; }

    public string Title { get; }

    internal MetadataPart Part { get; }

    /// <summary>What the part lists as a grid would: the debug directory a directory, and a table shown
    /// by its row count or a debug entry a table.</summary>
    public MetadataGridRole Role => Part switch
    {
        MetadataPart.Grid grid => grid.Role,
        MetadataPart.DebugDirectory => MetadataGridRole.Directory,
        MetadataPart.RowCount or MetadataPart.DecodedDebugEntry or MetadataPart.RawDebugEntry => MetadataGridRole.Table,
        _ => throw new UnreachableException(),
    };

    /// <summary>The grid shown, made the first time it is asked for; null for a part shown as text.</summary>
    public MetadataGrid? Grid => Part switch
    {
        MetadataPart.Grid grid => grid.View.Value,
        MetadataPart.DebugDirectory directory => directory.View.Value,
        MetadataPart.RowCount or MetadataPart.DecodedDebugEntry or MetadataPart.RawDebugEntry => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>The table whose rows the grid lists; null for a part that lists no table's rows.</summary>
    public TableIndex? ListedTable => Part switch
    {
        MetadataPart.Grid grid => grid.Schema.Table,
        MetadataPart.DebugDirectory or MetadataPart.RowCount or MetadataPart.DecodedDebugEntry or MetadataPart.RawDebugEntry => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>Whether this is a table with no rows, which the tree usually hides.</summary>
    public bool IsEmptyTable => Part is MetadataPart.RowCount count && count.Rows == 0;

    internal static MetadataTableInfo GridOf(LoadedModule module, MetadataFile file, string title, MetadataGridRole role, GridSchema schema) =>
        new(module, file, title, new MetadataPart.Grid(role, schema, new Lazy<MetadataGrid>(() => new MetadataGrid(title, module, file, schema))));

    internal static MetadataTableInfo DebugDirectoryOf(LoadedModule module, PEFile file)
    {
        const string title = "Debug Directory";
        var schema = PEHeaderEntries.DebugDirectory(file);
        return new(module, file, title, new MetadataPart.DebugDirectory(file, new Lazy<MetadataGrid>(() => new MetadataGrid(title, module, file, schema))));
    }

    internal static MetadataTableInfo RowCountOf(LoadedModule module, MetadataFile file, TableIndex table, int rows) =>
        new(module, file, $"{(byte)table:X2} {table} ({rows})", new MetadataPart.RowCount(table, rows));

    internal static MetadataTableInfo DecodedEntryOf(LoadedModule module, PEFile file, DebugDirectoryEntry entry, IReadOnlyList<string> lines) =>
        new(module, file, entry.Type.ToString(), new MetadataPart.DecodedDebugEntry(lines));

    internal static MetadataTableInfo RawEntryOf(LoadedModule module, PEFile file, DebugDirectoryEntry entry) =>
        new(module, file, entry.Type.ToString(), new MetadataPart.RawDebugEntry(file, entry));

    /// <summary>A part shown as text, as comment lines: a table's row count, or a debug entry decoded or
    /// as its raw bytes in hex; a grid's title alone.</summary>
    internal IEnumerable<string> Lines() => Part switch
    {
        MetadataPart.Grid or MetadataPart.DebugDirectory => [Title],
        MetadataPart.RowCount count => [$"{count.Table}: {count.Rows} rows"],
        MetadataPart.DecodedDebugEntry decoded => decoded.Lines.Prepend(Title),
        MetadataPart.RawDebugEntry raw => RawLines(raw),
        _ => throw new UnreachableException(),
    };

    private IEnumerable<string> RawLines(MetadataPart.RawDebugEntry raw)
    {
        yield return Title;
        var entry = raw.Entry;
        if (entry.DataSize > 0)
        {
            yield return $"Raw Data ({entry.DataSize}):";
            var reader = raw.File.Reader;
            var offset = reader.IsLoadedImage ? entry.DataRelativeVirtualAddress : entry.DataPointer;
            var data = reader.GetEntireImage().GetContent(offset, entry.DataSize);
            yield return Convert.ToHexStringLower(data.AsSpan());
        }
        else
        {
            yield return "(no data)";
        }
    }
}

/// <summary>Where a token cell leads: a row of a table node.</summary>
public readonly record struct MetadataTarget(BrowseNode Node, int Row);
