using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Core;

internal sealed partial class SubjectNode
{
    /// <summary>The node of the metadata of <paramref name="file"/>, titled <paramref name="title"/>, under <paramref name="parent"/>.</summary>
    internal static SubjectNode MetadataFolder(LoadedModule module, BrowseNode parent, MetadataFile file, string title) =>
        new(parent, new NodeSubject.Metadata(new MetadataFolderInfo(module, file, title)));

    private SubjectNode Part(MetadataTableInfo part) => new(this, new NodeSubject.MetadataTable(part));

    /// <summary>For a PE file its headers and debug directory, then the tables and the four heaps.</summary>
    private BrowseNode[] ListMetadata(MetadataFolderInfo folder)
    {
        var file = folder.File;
        var result = new List<BrowseNode>();
        if (file is PEFile pe)
        {
            foreach (var header in (ReadOnlySpan<GridSchema>)[PEHeaderEntries.Dos(pe), PEHeaderEntries.Coff(pe), PEHeaderEntries.Optional(pe)])
                result.Add(Part(MetadataTableInfo.GridOf(folder.Module, file, header.Key, MetadataGridRole.Header, header)));
            var directories = PEHeaderEntries.DataDirectories(pe);
            result.Add(Part(MetadataTableInfo.GridOf(folder.Module, file, directories.Key, MetadataGridRole.Directory, directories)));
            result.Add(Part(MetadataTableInfo.DebugDirectoryOf(folder.Module, pe)));
        }
        result.Add(new SubjectNode(this, new NodeSubject.MetadataTables(folder)));
        foreach (var (title, heap) in MetadataHeaps.Create(file))
            result.Add(Part(MetadataTableInfo.GridOf(folder.Module, file, title, MetadataGridRole.Heap, heap)));
        return result.ToArray();
    }

    /// <summary>A node per table: the grid of its rows. An empty table, whose grid cannot always be read,
    /// and a table ILSpy does not list show only how many rows there are.</summary>
    private BrowseNode[] ListTables(MetadataFolderInfo folder)
    {
        var file = folder.File;
        var result = new List<BrowseNode>();
        foreach (var index in Enum.GetValues<TableIndex>())
        {
            var rows = file.Metadata.GetTableRowCount(index);
            var schema = rows == 0 ? null : MetadataTables.Create(index, file);
            if (schema == null)
                result.Add(Part(MetadataTableInfo.RowCountOf(folder.Module, file, index, rows)));
            else
                result.Add(Part(MetadataTableInfo.GridOf(folder.Module, file, $"{(byte)index:X2} {index} ({rows})", MetadataGridRole.Table, schema)));
        }
        return result.ToArray();
    }

    /// <summary>The entries of the debug directory: what ILSpy decodes of each, else its raw bytes; the
    /// embedded PDB as the metadata folder of the debug metadata the module already loaded.</summary>
    private BrowseNode[] ListDebugEntries(LoadedModule module, PEFile file)
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
                        result.Add(Part(MetadataTableInfo.DecodedEntryOf(module, file, entry,
                            [$"GUID: {codeView.Guid}", $"Age: {codeView.Age}", $"Path: {codeView.Path}"])));
                        continue;
                    case DebugDirectoryEntryType.PdbChecksum:
                        var checksum = reader.ReadPdbChecksumDebugDirectoryData(entry);
                        result.Add(Part(MetadataTableInfo.DecodedEntryOf(module, file, entry,
                            [$"AlgorithmName: {checksum.AlgorithmName}", $"Checksum: {Convert.ToHexStringLower(checksum.Checksum.AsSpan())}"])));
                        continue;
                    case DebugDirectoryEntryType.EmbeddedPortablePdb:
                        var symbols = EmbeddedSymbols(module);
                        if (symbols != null)
                            result.Add(MetadataFolder(module, this, symbols.File, symbols.Title));
                        continue;
                }
            }
            catch (BadImageFormatException)
            {
            }
            result.Add(Part(MetadataTableInfo.RawEntryOf(module, file, entry)));
        }
        return result.ToArray();
    }

    /// <summary>The embedded PDB the module already loaded, or null when it could not be read.</summary>
    private static DebugSymbols? EmbeddedSymbols(LoadedModule module)
    {
        var symbols = module.DebugSymbols;
        if (symbols == null)
            return null;
        if (!symbols.File.IsEmbedded)
            return null;
        return symbols;
    }

    private static NodeKind KindOf(MetadataTableInfo part) => part.Part switch
    {
        MetadataPart.Grid grid => grid.Role switch
        {
            MetadataGridRole.Header => NodeKind.MetadataHeader,
            MetadataGridRole.Directory => NodeKind.MetadataDirectory,
            MetadataGridRole.Table => NodeKind.MetadataTable,
            MetadataGridRole.Heap => NodeKind.MetadataHeap,
            _ => throw new System.Diagnostics.UnreachableException(),
        },
        MetadataPart.DebugDirectory => NodeKind.MetadataDirectory,
        MetadataPart.RowCount or MetadataPart.DecodedDebugEntry or MetadataPart.RawDebugEntry => NodeKind.MetadataTable,
        _ => throw new System.Diagnostics.UnreachableException(),
    };
}
