using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Tests;

public sealed class MetadataTests
{
    private static BrowseNode Metadata(LoadedAssembly assembly) => assembly.Root.Child("Metadata");

    private static BrowseNode Table(LoadedAssembly assembly, string name) => Metadata(assembly).Child("Tables").Table(name);

    private static MetadataGrid Grid(BrowseNode node) => Assert.IsType<GridContent>(node.Content()).Grid;

    private static MetadataGrid TypeDefs() => Grid(Table(Trees.TestAssembly, "TypeDef"));

    [Fact]
    public void An_assembly_lists_its_metadata_and_its_portable_pdb_first()
    {
        var assembly = Trees.TestAssembly;

        var first = assembly.Root.Children.Take(3).Select(c => (c.Kind, c.Text));

        Assert.Equal(
            [(NodeKind.Metadata, "Metadata"), (NodeKind.Metadata, "Debug Metadata (From portable PDB)"), (NodeKind.ReferencesFolder, "References")],
            first);
    }

    [Fact]
    public void The_metadata_folder_lists_headers_directories_tables_and_heaps_in_ILSpys_order()
    {
        var assembly = Trees.TestAssembly;

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
    public void Every_table_is_listed_and_an_empty_one_shows_only_that_it_has_no_rows()
    {
        var assembly = Trees.TestAssembly;

        var tables = Metadata(assembly).Child("Tables").Children;
        var empty = Table(assembly, "FieldPtr");

        Assert.Equal(Enum.GetValues<TableIndex>().Length, tables.Count);
        Assert.True(empty.IsEmptyTable);
        Assert.Equal("03 FieldPtr (0)", empty.Text);
        Assert.Contains("FieldPtr: 0 rows", empty.Decompile(DecompilationLanguage.CSharp).GetText());
        Assert.False(Table(assembly, "TypeDef").IsEmptyTable);
    }

    [Fact]
    public void Every_grid_reads_every_cell_tooltip_and_link()
    {
        var assembly = Trees.TestAssembly;
        var grids = 0;

        foreach (var root in assembly.Root.Children.Where(c => c.Kind == NodeKind.Metadata))
            grids += Sweep(root);

        Assert.True(grids > 20, $"{grids} grids");

        static int Sweep(BrowseNode node)
        {
            var count = 0;
            var content = node.Content();
            if (content is GridContent { Grid: var grid })
            {
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
                Assert.False(Assert.IsType<DocumentContent>(content).Document.IsError);
            }
            foreach (var child in node.Children)
                count += Sweep(child);
            return count;
        }
    }

    [Fact]
    public void The_TypeDef_table_shows_ILSpys_columns_in_hex()
    {
        var grid = TypeDefs();
        var row = grid.RowNamed("Calculator`1");

        Assert.Equal(["RID", "Token", "Offset", "Attributes", "Name", "Namespace", "BaseType", "FieldList", "MethodList"], grid.Columns.Select(c => c.Name));
        Assert.Equal((row + 1).ToString(), grid.Text(row, grid.Column("RID")));
        Assert.Equal($"02{row + 1:X6}", grid.Text(row, grid.Column("Token")));
        Assert.Equal(Trees.FixturesNamespace, grid.Text(row, grid.Column("Namespace")));
        Assert.True(grid.Columns[grid.Column("BaseType")].IsToken);
        Assert.False(grid.Columns[grid.Column("Name")].IsToken);
    }

    [Fact]
    public void A_flags_cell_explains_each_group_of_its_bits()
    {
        var grid = TypeDefs();

        var tooltip = grid.Tooltip(grid.RowNamed("Calculator`1"), grid.Column("Attributes"))!;

        Assert.Equal(["Visibility", "Layout", "ClassSemantics", "StringFormat", "Flags"], tooltip.Groups.Select(g => g.Header));
        var visibility = tooltip.Groups[0];
        Assert.True(visibility.IsChoice);
        Assert.Equal("Public (0001)", Assert.Single(visibility.Flags).Name);
        Assert.Equal("AutoLayout (0000)", Assert.Single(tooltip.Groups[1].Flags).Name);
        var flags = tooltip.Groups[^1];
        Assert.False(flags.IsChoice);
        Assert.Contains(new MetadataFlag("BeforeFieldInit (100000)", true), flags.Flags);
        Assert.Contains(new MetadataFlag("Abstract (0080)", false), flags.Flags);
        Assert.Contains(new MetadataFlag("IsTypeForwarder (200000)", false), flags.Flags);
    }

    [Fact]
    public void A_flags_schema_keeps_bits_that_combine_out_of_its_choice_groups()
    {
        var method = FlagsSchemaInferer.For(typeof(System.Reflection.MethodAttributes));
        var generic = FlagsSchemaInferer.For(typeof(System.Reflection.GenericParameterAttributes));
        var import = FlagsSchemaInferer.For(typeof(System.Reflection.MethodImportAttributes));

        Assert.Equal(["MemberAccess", "VtableLayout"], method.MutexGroups.Select(g => g.Name));
        Assert.Contains(method.IndependentFlags, f => f.Name == "RTSpecialName");
        Assert.Equal(["Variance"], generic.MutexGroups.Select(g => g.Name));
        Assert.Equal("None (0000)", generic.MutexGroups[0].Values[0].Label);
        Assert.Contains(generic.IndependentFlags, f => f.Name == "DefaultConstructorConstraint");
        Assert.Equal(["None (0000)", "Ansi (0002)", "Unicode (0004)", "Auto (0006)"], import.MutexGroups.Single(g => g.Name == "CharSet").Values.Select(v => v.Label));
        Assert.True(FlagsSchemaInferer.IsFlags(typeof(System.Reflection.MethodImplAttributes)));
        Assert.DoesNotContain(FlagsSchemaInferer.For(typeof(System.Reflection.MethodImplAttributes)).IndependentFlags, f => f.Name == "MaxMethodImplVal");
        Assert.False(FlagsSchemaInferer.IsFlags(typeof(System.Reflection.AssemblyHashAlgorithm)));
    }

    [Fact]
    public void A_token_cell_links_to_the_row_of_the_table_it_names()
    {
        var assembly = Trees.TestAssembly;
        var grid = Grid(Table(assembly, "TypeDef"));
        var row = grid.RowNamed("Calculator`1");

        var target = grid.Link(row, grid.Column("BaseType"))!.Value;

        Assert.Same(Table(assembly, "TypeRef"), target.Node);
        var typeRefs = Grid(target.Node);
        Assert.Equal("Object", typeRefs.Text(target.Row, typeRefs.Column("Name")));
        Assert.Equal(target.Row, typeRefs.RowOf(Convert.ToInt32(grid.Text(row, grid.Column("BaseType")), 16)));
    }

    [Fact]
    public void A_row_goes_to_the_definition_its_token_names()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var types = Grid(Table(assembly, "TypeDef"));
        var references = Grid(Table(assembly, "TypeRef"));

        var calculator = types.Definition(types.RowNamed("Calculator`1"))!;
        var obj = references.Definition(references.RowNamed("Object"))!;

        Assert.Same(assembly.Calculator(), list.FindNode(calculator));
        Assert.Equal("Object", list.FindNode(obj)!.Text);
        Assert.Null(Grid(Metadata(assembly).Child("DOS Header")).Definition(0));
    }

    [Fact]
    public void Sorting_orders_rows_by_a_column_keeping_ties_in_table_order()
    {
        var grid = TypeDefs();
        var name = grid.Column("Name");

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
        var assembly = Trees.TestAssembly;
        var directory = Metadata(assembly).Child("Debug Directory");

        var codeView = directory.Child("CodeView").Decompile(DecompilationLanguage.CSharp).GetText();

        Assert.Contains("// GUID: ", codeView);
        Assert.Contains("// Path: ", codeView);
        Assert.Contains("CodeView", Enumerable.Range(0, Grid(directory).RowCount).Select(r => Grid(directory).Text(r, Grid(directory).Column("Type"))));
    }

    [Fact]
    public void The_debug_directory_shows_the_embedded_pdb_the_assembly_already_loaded()
    {
        var assembly = new AssemblyList().Open(Trees.EmbeddedPdbPath);
        var atRoot = (MetadataFolderNode)assembly.Root.Child("Debug Metadata (Embedded)");

        var directory = Metadata(assembly).Child("Debug Directory");
        var embedded = (MetadataFolderNode)directory.Child("Debug Metadata (Embedded)");

        Assert.Same(atRoot.File, embedded.File);
    }

    [Fact]
    public void The_tables_folder_counts_the_rows_of_each_table()
    {
        var assembly = Trees.TestAssembly;

        var text = Metadata(assembly).Child("Tables").Decompile(DecompilationLanguage.CSharp).GetText();

        Assert.StartsWith("// Tables", text);
        Assert.Contains("// 02 TypeDef: ", text);
    }

    private static string[] NamesOf(MetadataGrid grid, int[] rows) => rows.Select(r => grid.Text(r, grid.Column("Name"))).ToArray();

    private static MetadataColumnFilter?[] Filters(MetadataGrid grid, string column, MetadataColumnFilter filter)
    {
        var filters = new MetadataColumnFilter?[grid.Columns.Count];
        filters[grid.Column(column)] = filter;
        return filters;
    }

    [Fact]
    public void A_text_filter_matches_part_of_a_cell_ignoring_case_or_a_regex()
    {
        var grid = TypeDefs();

        var part = NamesOf(grid, grid.Filter(null, Filters(grid, "Name", new MetadataColumnFilter { Text = "calcULATOR" })));
        var regex = NamesOf(grid, grid.Filter(null, Filters(grid, "Name", new MetadataColumnFilter { Text = "/^(Circle|Size)$/" })));

        Assert.Contains("Calculator`1", part);
        Assert.All(part, n => Assert.Contains("calculator", n, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(["Circle", "Size"], regex.Order());
    }

    [Fact]
    public void A_number_filter_compares_with_an_operator_and_otherwise_matches_the_hex_shown()
    {
        var grid = TypeDefs();
        var rid = grid.Column("RID");

        var above = grid.Filter(null, Filters(grid, "RID", new MetadataColumnFilter { Text = "> 0x3" }));
        var shown = grid.Filter(null, Filters(grid, "Token", new MetadataColumnFilter { Text = "02000002" }));

        Assert.All(above, r => Assert.True(int.Parse(grid.Text(r, rid)) > 3));
        Assert.Equal(grid.RowCount - 3, above.Length);
        Assert.Equal([1], shown);
    }

    [Fact]
    public void A_flags_filter_picks_values_of_a_group_and_requires_or_excludes_flags()
    {
        var grid = TypeDefs();
        var schema = grid.Columns[grid.Column("Attributes")].Flags!;
        var state = new FilterState(schema);
        var filters = Filters(grid, "Attributes", new MetadataColumnFilter { Flags = state });

        state.SetMutexSelection("Visibility", [(uint)System.Reflection.TypeAttributes.NotPublic]);
        state.SetFlagState("Abstract", TriState.Excluded);
        var names = NamesOf(grid, grid.Filter(null, filters));

        Assert.Contains("Visibility", schema.MutexGroups.Select(g => g.Name));
        Assert.Contains("Circle", names);
        Assert.DoesNotContain("Calculator`1", names);
        Assert.DoesNotContain("<>c", names);
        Assert.Equal("Visibility = NotPublic ∧ ¬Abstract", FilterStatePresenter.Describe(state));
    }

    [Fact]
    public void Filters_keep_the_sorted_order()
    {
        var grid = TypeDefs();
        var order = grid.Order(grid.Column("Name"), descending: true);

        var rows = grid.Filter(order, Filters(grid, "Namespace", new MetadataColumnFilter { Text = "Fixtures" }));

        Assert.Equal(order.Where(rows.Contains), rows);
    }

    [Fact]
    public void A_flags_filter_round_trips_through_its_saved_form()
    {
        var schema = FlagsSchemaInferer.For(typeof(System.Reflection.TypeAttributes));
        var state = new FilterState(schema);
        state.SetMutexSelection("Visibility", [1u, 2u]);
        state.SetFlagState("Abstract", TriState.Required);
        state.IndependentMode = MatchMode.Any;

        var saved = state.Save()!;
        var restored = new FilterState(schema);
        restored.Restore(saved with { Groups = new(saved.Groups!) { ["Gone"] = [1u] }, Flags = new(saved.Flags!) { ["Missing"] = TriState.Required } });

        Assert.Equal(FilterStatePresenter.Describe(state), FilterStatePresenter.Describe(restored));
        Assert.Equal(MatchMode.Any, restored.IndependentMode);
        Assert.Equal(["Abstract"], saved.Flags!.Keys);
        Assert.Equal(["Visibility"], saved.Groups!.Keys);
        Assert.Null(new FilterState(schema).Save());
    }

    [Fact]
    public void A_header_breaks_the_bits_of_its_flags_down_on_hovering_their_value()
    {
        var assembly = Trees.TestAssembly;
        var coff = Grid(Metadata(assembly).Child("COFF Header"));
        var characteristics = Enumerable.Range(0, coff.RowCount).Single(r => coff.Text(r, coff.Column("Member")) == "Characteristics");

        var bits = Assert.Single(coff.Tooltip(characteristics, coff.Column("Value"))!.Groups).Flags;

        Assert.False(coff.HasDetails);
        Assert.Equal(16, bits.Count);
        Assert.Contains(new MetadataFlag("File is executable (0002)", true), bits);
        Assert.Null(coff.Tooltip(0, coff.Column("Value")));
        Assert.False(Grid(Table(assembly, "TypeDef")).HasDetails);
    }

    [Fact]
    public void Debug_information_shows_its_decoded_value_under_the_selected_row()
    {
        var assembly = Trees.TestAssembly;
        var pdb = assembly.Root.Child("Debug Metadata (From portable PDB)").Child("Tables");
        var grid = Grid(pdb.Table("CustomDebugInformation"));
        var kind = grid.Column("KindString");

        var options = Enumerable.Range(0, grid.RowCount).Single(r => grid.Text(r, kind).StartsWith("Compilation Options"));
        var table = Assert.IsType<MetadataTableDetails>(grid.Details(options));

        Assert.True(grid.HasDetails);
        Assert.Equal(["Name", "Value"], table.Headers);
        Assert.Contains(table.Rows, r => r[0] == "language" && r[1] == "C#");
    }

    private static (int, int)[] Cells(BrowseNode table, string first, string second)
    {
        var grid = Grid(table);
        return Enumerable.Range(0, grid.RowCount).Select(r => (Hex(grid, r, first), Hex(grid, r, second))).ToArray();
    }

    private static int Hex(MetadataGrid grid, int row, string column) => Convert.ToInt32(grid.Text(row, grid.Column(column)), 16);

    private static int Token(EntityHandle handle) => MetadataTokens.GetToken(handle);

    [Fact]
    public void CoreLibs_relationship_tables_hold_the_rows_the_metadata_reader_decodes()
    {
        var corelib = new AssemblyList().Open(Trees.CoreLibPath);
        var md = corelib.File.Metadata;
        var types = md.TypeDefinitions.Select(t => (Handle: t, Type: md.GetTypeDefinition(t))).ToArray();
        var fields = md.FieldDefinitions.Select(f => (Handle: f, Field: md.GetFieldDefinition(f))).ToArray();

        Assert.Equal(
            types.Where(t => !t.Type.GetDeclaringType().IsNil).Select(t => (Token(t.Handle), Token(t.Type.GetDeclaringType()))).Order(),
            Cells(Table(corelib, "NestedClass"), "NestedClass", "EnclosingClass").Order());

        Assert.Equal(
            types.SelectMany(t => t.Type.GetInterfaceImplementations().Select(i => (Token(t.Handle), Token(md.GetInterfaceImplementation(i).Interface)))).Order(),
            Cells(Table(corelib, "InterfaceImpl"), "Class", "Interface").Order());

        Assert.Equal(
            fields.Where(f => f.Field.GetOffset() >= 0).Select(f => (Token(f.Handle), f.Field.GetOffset())),
            Cells(Table(corelib, "FieldLayout"), "Field", "FieldOffset"));

        Assert.Equal(
            fields.Where(f => f.Field.GetRelativeVirtualAddress() != 0).Select(f => (Token(f.Handle), f.Field.GetRelativeVirtualAddress())),
            Cells(Table(corelib, "FieldRva"), "Field", "RVA"));

        Assert.Equal(
            types.Where(t => t.Type.GetEvents().Count > 0).Select(t => (Token(t.Handle), Token(t.Type.GetEvents().First()))),
            Cells(Table(corelib, "EventMap"), "Parent", "EventList"));

        Assert.Equal(
            types.Where(t => t.Type.GetProperties().Count > 0).Select(t => (Token(t.Handle), Token(t.Type.GetProperties().First()))),
            Cells(Table(corelib, "PropertyMap"), "Parent", "PropertyList"));

        var layouts = Grid(Table(corelib, "ClassLayout"));
        Assert.NotEqual(0, layouts.RowCount);
        for (var row = 0; row < layouts.RowCount; row++)
        {
            var layout = md.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.EntityHandle(Hex(layouts, row, "Parent"))).GetLayout();
            Assert.Equal((int.Parse(layouts.Text(row, layouts.Column("PackingSize"))), int.Parse(layouts.Text(row, layouts.Column("ClassSize")))), (layout.PackingSize, layout.Size));
        }
    }

    [Fact]
    public void The_StateMachineMethod_table_pairs_each_MoveNext_with_its_kickoff_method()
    {
        var assembly = Trees.TestAssembly;
        using var stream = File.OpenRead(Path.ChangeExtension(Trees.TestAssemblyPath, ".pdb"));
        var md = MetadataReaderProvider.FromPortablePdbStream(stream).GetMetadataReader();
        var tables = assembly.Root.Child("Debug Metadata (From portable PDB)").Child("Tables");

        var rows = Cells(tables.Table("StateMachineMethod"), "MoveNextMethod", "KickoffMethod");

        Assert.NotEmpty(rows);
        Assert.Equal(
            md.MethodDebugInformation.Select(h => (MoveNext: h.ToDefinitionHandle(), Kickoff: md.GetMethodDebugInformation(h).GetStateMachineKickoffMethod()))
                .Where(m => !m.Kickoff.IsNil).Select(m => (Token(m.MoveNext), Token(m.Kickoff))),
            rows);
    }

    [Fact]
    public void The_edit_and_continue_tables_list_each_logged_change_and_mapped_token()
    {
        using var temp = new TempDir();
        var assembly = new AssemblyList().Open(WriteEditAndContinueAssembly(temp));
        var type = MetadataTokens.GetToken(MetadataTokens.TypeDefinitionHandle(1));

        var log = Grid(Table(assembly, "EncLog"));
        var map = Grid(Table(assembly, "EncMap"));

        Assert.Equal(["RID", "Token", "Offset", "Handle", "FuncCode"], log.Columns.Select(c => c.Name));
        Assert.Equal(2, log.RowCount);
        Assert.Equal(type, Hex(log, 0, "Handle"));
        Assert.Equal((int)EditAndContinueOperation.AddMethod, Hex(log, 1, "FuncCode"));
        Assert.Equal(nameof(EditAndContinueOperation.AddMethod), log.Tooltip(1, log.Column("FuncCode"))!.Text);
        Assert.Equal(1, map.RowCount);
        Assert.Equal(type, Hex(map, 0, "Handle"));
        Assert.Equal(new MetadataTarget(Table(assembly, "TypeDef"), 0), map.Link(0, map.Column("Handle")));
    }

    /// <summary>An assembly whose metadata logs two changes to its one type, as an edit and continue
    /// delta does, and maps that type's row.</summary>
    private static string WriteEditAndContinueAssembly(TempDir temp)
    {
        var metadata = new MetadataBuilder();
        var name = metadata.GetOrAddString("EditAndContinue");
        metadata.AddModule(0, name, metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(name, new Version(1, 0), default, default, 0, AssemblyHashAlgorithm.None);
        var firstField = MetadataTokens.FieldDefinitionHandle(1);
        var firstMethod = MetadataTokens.MethodDefinitionHandle(1);
        var type = metadata.AddTypeDefinition(default, default, metadata.GetOrAddString("<Module>"), default, firstField, firstMethod);
        metadata.AddEncLogEntry(type, EditAndContinueOperation.Default);
        metadata.AddEncLogEntry(type, EditAndContinueOperation.AddMethod);
        metadata.AddEncMapEntry(type);

        var pe = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
        var image = new BlobBuilder();
        pe.Serialize(image);
        return temp.Write("EditAndContinue.dll", image.ToArray());
    }

    [Fact]
    public void CoreLibs_tables_and_heaps_read()
    {
        var list = new AssemblyList();
        var corelib = list.Open(Trees.CoreLibPath);

        foreach (var node in Metadata(corelib).Child("Tables").Children.Concat(Metadata(corelib).Children.Where(c => c.Kind == NodeKind.MetadataHeap)))
        {
            if (node.Content() is not GridContent { Grid: var grid }) continue;
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
