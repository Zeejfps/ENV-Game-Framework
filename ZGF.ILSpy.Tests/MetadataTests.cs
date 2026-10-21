using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Tests;

public sealed class MetadataTests
{
    private static BrowseNode Metadata(LoadedAssembly assembly) => assembly.Root.Child("Metadata");

    private static BrowseNode Table(LoadedAssembly assembly, string name) =>
        Metadata(assembly).Child("Tables").Children.Single(c => c.Text.Split(' ')[1] == name);

    private static MetadataGrid Grid(BrowseNode node) => node.LoadGridAsync().GetAwaiter().GetResult();

    private static int Column(MetadataGrid grid, string name) => grid.Columns.Select(c => c.Name).ToList().IndexOf(name);

    private static int RowNamed(MetadataGrid grid, string name)
    {
        var column = Column(grid, "Name");
        for (var row = 0; row < grid.RowCount; row++)
        {
            if (grid.Text(row, column) == name) return row;
        }
        throw new InvalidOperationException($"No row named {name}");
    }

    [Fact]
    public void An_assembly_lists_its_metadata_and_its_portable_pdb_first()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var first = assembly.Root.Children.Take(3).Select(c => (c.Kind, c.Text));

        Assert.Equal(
            [(NodeKind.Metadata, "Metadata"), (NodeKind.Metadata, "Debug Metadata (From portable PDB)"), (NodeKind.ReferencesFolder, "References")],
            first);
    }

    [Fact]
    public void The_metadata_folder_lists_headers_directories_tables_and_heaps_in_ILSpys_order()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var children = Metadata(assembly).Children;

        Assert.Equal(
            [
                (NodeKind.MetadataHeader, "DOS Header"), (NodeKind.MetadataHeader, "COFF Header"), (NodeKind.MetadataHeader, "Optional Header"),
                (NodeKind.MetadataDirectory, "Data Directories"), (NodeKind.MetadataDirectory, "Debug Directory"), (NodeKind.MetadataTables, "Tables"),
            ],
            children.Take(6).Select(c => (c.Kind, c.Text)));
        Assert.Equal(["String Heap", "UserString Heap", "Guid Heap", "Blob Heap"], children.Skip(6).Select(c => c.Text[..c.Text.IndexOf(" (")]));
        Assert.All(children.Skip(6), c => Assert.Equal(NodeKind.MetadataHeap, c.Kind));
        Assert.Contains(Metadata(assembly).Child("Tables").Children, c => c.Text.StartsWith("02 TypeDef (", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_grid_reads_every_cell_tooltip_and_link()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var grids = 0;

        foreach (var root in assembly.Root.Children.Where(c => c.Kind == NodeKind.Metadata))
            grids += Sweep(root);

        Assert.True(grids > 20, $"{grids} grids");

        static int Sweep(BrowseNode node)
        {
            var count = 0;
            if (node.ShowsGrid)
            {
                var grid = Grid(node);
                for (var row = 0; row < grid.RowCount; row++)
                {
                    for (var column = 0; column < grid.Columns.Count; column++)
                    {
                        Assert.NotNull(grid.Text(row, column));
                        grid.Tooltip(row, column);
                        if (grid.Link(row, column) is { } target)
                            Assert.InRange(target.Row, 0, Grid(target.Node).RowCount - 1);
                    }
                    grid.Definition(row);
                }
                count++;
            }
            else
            {
                Assert.False(node.Decompile(DecompilationLanguage.CSharp).IsError);
            }
            foreach (var child in node.Children)
                count += Sweep(child);
            return count;
        }
    }

    [Fact]
    public void The_TypeDef_table_shows_ILSpys_columns_in_hex()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var grid = Grid(Table(assembly, "TypeDef"));
        var row = RowNamed(grid, "Calculator`1");

        Assert.Equal(["RID", "Token", "Offset", "Attributes", "Name", "Namespace", "BaseType", "FieldList", "MethodList"], grid.Columns.Select(c => c.Name));
        Assert.Equal((row + 1).ToString(), grid.Text(row, Column(grid, "RID")));
        Assert.Equal($"02{row + 1:X6}", grid.Text(row, Column(grid, "Token")));
        Assert.Equal(Trees.FixturesNamespace, grid.Text(row, Column(grid, "Namespace")));
        Assert.True(grid.Columns[Column(grid, "BaseType")].IsToken);
        Assert.False(grid.Columns[Column(grid, "Name")].IsToken);
    }

    [Fact]
    public void A_flags_cell_explains_each_group_of_its_bits()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var grid = Grid(Table(assembly, "TypeDef"));

        var tooltip = grid.Tooltip(RowNamed(grid, "Calculator`1"), Column(grid, "Attributes"))!;

        var visibility = tooltip.Groups.Single(g => g.Header == "Visibility: ");
        Assert.True(visibility.IsChoice);
        Assert.Equal("Public (0001)", Assert.Single(visibility.Flags).Name);
        var flags = tooltip.Groups.Single(g => g.Header == "Flags:");
        Assert.False(flags.IsChoice);
        Assert.Contains(flags.Flags, f => f.Name.StartsWith("BeforeFieldInit") && f.IsSet);
        Assert.Contains(flags.Flags, f => f.Name.StartsWith("Abstract") && !f.IsSet);
    }

    [Fact]
    public void A_token_cell_links_to_the_row_of_the_table_it_names()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var grid = Grid(Table(assembly, "TypeDef"));
        var row = RowNamed(grid, "Calculator`1");

        var target = grid.Link(row, Column(grid, "BaseType"))!.Value;

        Assert.Same(Table(assembly, "TypeRef"), target.Node);
        var typeRefs = Grid(target.Node);
        Assert.Equal("Object", typeRefs.Text(target.Row, Column(typeRefs, "Name")));
        Assert.Equal(target.Row, typeRefs.RowOf(Convert.ToInt32(grid.Text(row, Column(grid, "BaseType")), 16)));
    }

    [Fact]
    public void A_row_goes_to_the_definition_its_token_names()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var types = Grid(Table(assembly, "TypeDef"));
        var references = Grid(Table(assembly, "TypeRef"));

        var calculator = types.Definition(RowNamed(types, "Calculator`1"))!;
        var obj = references.Definition(RowNamed(references, "Object"))!;

        Assert.Same(assembly.Calculator(), list.FindNode(calculator));
        Assert.Equal("Object", list.FindNode(obj)!.Text);
        Assert.Null(Grid(Metadata(assembly).Child("DOS Header")).Definition(0));
    }

    [Fact]
    public void Sorting_orders_rows_by_a_column_keeping_ties_in_table_order()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var grid = Grid(Table(assembly, "TypeDef"));
        var name = Column(grid, "Name");

        var ascending = grid.Order(name, descending: false).Select(r => grid.Text(r, name)).ToList();
        var descending = grid.Order(name, descending: true).Select(r => grid.Text(r, name)).ToList();

        Assert.Equal(ascending.Order(StringComparer.CurrentCulture), ascending);
        Assert.Equal(ascending.OrderDescending(StringComparer.CurrentCulture), descending);
        var ties = grid.Order(name, descending: true).Where(r => grid.Text(r, name) == "<>c").ToList();
        Assert.Equal(ties.Order(), ties);
    }

    [Fact]
    public void The_debug_directory_decodes_its_CodeView_entry()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var directory = Metadata(assembly).Child("Debug Directory");

        var codeView = directory.Child("CodeView").Decompile(DecompilationLanguage.CSharp).GetText();

        Assert.Contains("// GUID: ", codeView);
        Assert.Contains("// Path: ", codeView);
        Assert.Contains("CodeView", Enumerable.Range(0, Grid(directory).RowCount).Select(r => Grid(directory).Text(r, Column(Grid(directory), "Type"))));
    }

    [Fact]
    public void The_tables_folder_counts_the_rows_of_each_table()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var text = Metadata(assembly).Child("Tables").Decompile(DecompilationLanguage.CSharp).GetText();

        Assert.StartsWith("// Tables", text);
        Assert.Contains("// 02 TypeDef: ", text);
    }

    [Fact]
    public void CoreLibs_tables_and_heaps_read()
    {
        var list = new AssemblyList();
        var corelib = list.Open(Trees.CoreLibPath);

        foreach (var node in Metadata(corelib).Child("Tables").Children.Concat(Metadata(corelib).Children.Where(c => c.Kind == NodeKind.MetadataHeap)))
        {
            if (!node.ShowsGrid) continue;
            var grid = Grid(node);
            for (var row = 0; row < Math.Min(grid.RowCount, 50); row++)
            {
                for (var column = 0; column < grid.Columns.Count; column++)
                {
                    grid.Text(row, column);
                    grid.Tooltip(row, column);
                    grid.Link(row, column);
                }
            }
        }
    }
}
