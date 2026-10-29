using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class DecompilationTests
{
    [Fact]
    public void CSharp_spans_are_classified()
    {
        var document = Trees.CalculatorDocument(DecompilationLanguage.CSharp);

        Assert.False(document.IsError);
        Assert.Equal("Calculator<T>", document.Title);
        var header = document.Lines[document.LineContaining("public class Calculator<T>")];
        Assert.Equal(SpanKind.Keyword, header.SpanAt("public").Kind);
        Assert.Equal(SpanKind.Type, header.SpanAt("Calculator").Kind);
        Assert.True(header.SpanAt("Calculator").IsDefinition);

        var describe = document.Lines[document.LineContaining("return \"Total: \"")];
        Assert.Equal(SpanKind.String, describe.SpanAt("\"Total: \"").Kind);
        Assert.Equal(SpanKind.Number, describe.SpanAt("42").Kind);
        Assert.Equal(SpanKind.Property, describe.SpanAt("Count").Kind);

        var usings = document.Lines[document.LineContaining("using System.Collections.Generic;")];
        Assert.Equal(SpanKind.Namespace, usings.SpanAt("Collections").Kind);

        var add = document.Lines[document.LineContaining("public void Add(T item)")];
        Assert.Equal(SpanKind.Method, add.SpanAt("Add").Kind);
        Assert.Equal(SpanKind.Parameter, add.SpanAt("item").Kind);
        var field = document.Lines[document.LineContaining("private readonly List<T> _items")];
        Assert.Equal(SpanKind.Field, field.SpanAt("_items").Kind);
        Assert.Equal(SpanKind.Event, document.Lines[document.LineContaining("public event")].SpanAt("Changed").Kind);
    }

    [Fact]
    public void Spans_cover_every_line_exactly()
    {
        foreach (var language in new[] { DecompilationLanguage.CSharp, DecompilationLanguage.IL })
        {
            var document = Trees.CalculatorDocument(language);
            foreach (var line in document.Lines)
            {
                var position = 0;
                foreach (var span in line.Spans)
                {
                    Assert.Equal(position, span.Start);
                    Assert.True(span.Length > 0);
                    position = span.End;
                }
                Assert.Equal(line.Text.Length, position);
                Assert.DoesNotContain('\n', line.Text);
            }
        }
    }

    [Fact]
    public void Member_nodes_decompile_on_their_own()
    {
        var assembly = Trees.TestAssembly;

        var document = assembly.Calculator().Child("Describe() : string").Decompile(DecompilationLanguage.CSharp);

        Assert.Equal("[Description(\"Summarizes the calculator\")]", document.Lines[document.LineContaining("internal string Describe()") - 1].Text);
        Assert.DoesNotContain(document.Lines.ToArray(), l => l.Text.Contains("class"));
    }

    [Fact]
    public void An_older_language_version_avoids_newer_syntax()
    {
        var assembly = Trees.TestAssembly;
        var calculator = assembly.Calculator();
        var csharp5 = new DecompilerOptions { LanguageVersion = CSharpVersion.CSharp5 };

        var latest = calculator.Child("Count : int").Decompile(DecompilationLanguage.CSharp).GetText();
        var older = calculator.Child("Count : int").Decompile(DecompilationLanguage.CSharp, csharp5).GetText();
        var ctor = calculator.Child("Calculator()").Decompile(DecompilationLanguage.CSharp, csharp5);

        Assert.Contains("public int Count => _items.Count;", latest);
        Assert.DoesNotContain("=>", older);
        Assert.Contains("return _items.Count;", older);
        Assert.False(ctor.IsError);
        Assert.Contains(ctor.Lines.ToArray(), l => l.Text.Contains("public Calculator()"));
    }

    [Fact]
    public void Toggles_come_from_the_decompiler_with_the_version_that_brought_each_feature()
    {
        var all = DecompilerToggle.All;

        Assert.Contains(all, t => t is { Name: "SwitchExpressions", Group: DecompilerToggleGroup.Language, IntroducedIn: CSharpVersion.CSharp8, DefaultValue: true });
        Assert.Contains(all, t => t is { Name: "AlwaysUseBraces", Group: DecompilerToggleGroup.Other, IntroducedIn: null });
        Assert.Contains(all, t => t is { Name: "RemoveDeadCode", Group: DecompilerToggleGroup.FSharp, DefaultValue: false });
        Assert.Contains(all, t => t is { Name: "ShowXmlDocumentation", Group: DecompilerToggleGroup.Other, DefaultValue: true });
        Assert.Contains(all, t => t is { Name: "UseDebugSymbols", Group: DecompilerToggleGroup.Other, DefaultValue: true });
        Assert.Contains(all, t => t is { Name: "AutoLoadAssemblyReferences", Group: DecompilerToggleGroup.Other, DefaultValue: true });
        Assert.DoesNotContain(all, t => t.Name is "DecompileMemberBodies");
    }

    [Fact]
    public void Locals_take_their_names_from_the_pdb_until_debug_symbols_are_off()
    {
        var grow = Trees.TestAssembly.Fixtures().Child("Circle", "Grow(double, int) : double");
        var off = DecompilerOptions.Default.With(DecompilerToggle.UseDebugSymbols, false);

        var named = grow.Decompile(DecompilationLanguage.CSharp).GetText();
        var guessed = grow.Decompile(DecompilationLanguage.CSharp, off).GetText();

        Assert.Contains("double grown = Radius;", named);
        Assert.Contains("for (int step = 0; step < times; step++)", named);
        Assert.DoesNotContain("double grown", guessed);
        Assert.DoesNotContain("int step", guessed);
    }

    [Fact]
    public void IL_names_locals_from_the_pdb_until_debug_symbols_are_off()
    {
        var grow = Trees.TestAssembly.Fixtures().Child("Circle", "Grow(double, int) : double");
        var off = DecompilerOptions.Default.With(DecompilerToggle.UseDebugSymbols, false);

        var named = grow.Decompile(DecompilationLanguage.IL).GetText();
        var unnamed = grow.Decompile(DecompilationLanguage.IL, off).GetText();

        Assert.Contains("[0] float64 grown,", named);
        Assert.Contains("[1] int32 step,", named);
        Assert.Contains("[0] float64,", unnamed);
    }

    [Fact]
    public void IL_comes_out_the_same_under_any_toggle_but_debug_symbols()
    {
        var braces = DecompilerToggle.Find("AlwaysUseBraces")!;
        var bracesOff = DecompilerOptions.Default.With(braces, false);
        var symbolsOff = DecompilerOptions.Default.With(DecompilerToggle.UseDebugSymbols, false);

        Assert.True(DecompilerOptions.Default.ProducesSameCode(bracesOff, DecompilationLanguage.IL));
        Assert.False(DecompilerOptions.Default.ProducesSameCode(bracesOff, DecompilationLanguage.CSharp));
        Assert.False(DecompilerOptions.Default.ProducesSameCode(bracesOff, DecompilationLanguage.ILWithCSharp));
        Assert.False(DecompilerOptions.Default.ProducesSameCode(symbolsOff, DecompilationLanguage.IL));
    }

    [Fact]
    public void IL_with_CSharp_writes_each_statement_above_its_first_instruction_in_CSharps_colors()
    {
        var document = CircleGrow(DecompilationLanguage.ILWithCSharp);

        var statement = document.LineContaining("// double grown = Radius;");
        var line = document.Lines[statement];
        var instruction = document.Lines[statement + 1];

        Assert.StartsWith("IL_0001: ldarg.0", instruction.Text.Trim());
        Assert.Equal(SpanKind.ILOpcode, document.SpanAt(statement + 1, instruction.Text.IndexOf("ldarg", StringComparison.Ordinal)).Kind);
        Assert.Equal(SpanKind.Keyword, line.SpanAt("double").Kind);
        Assert.Equal(SpanKind.Property, line.SpanAt("Radius").Kind);
        Assert.NotNull(line.SpanAt("Radius").Reference);
        Assert.Null(line.SpanAt("grown").Reference);
    }

    [Fact]
    public void IL_with_CSharp_dims_what_a_line_holds_besides_the_statement()
    {
        var document = CircleGrow(DecompilationLanguage.ILWithCSharp);

        var line = document.LineContaining("// for (int step = 0;");
        var text = document.Lines[line].Text;

        Assert.Equal(SpanKind.Comment, document.SpanAt(line, text.IndexOf("for (", StringComparison.Ordinal)).Kind);
        Assert.Equal(SpanKind.Keyword, document.SpanAt(line, text.IndexOf("int", StringComparison.Ordinal)).Kind);
        Assert.Equal(SpanKind.Comment, document.SpanAt(line, text.IndexOf("step++", StringComparison.Ordinal)).Kind);
    }

    [Fact]
    public void IL_with_CSharp_says_where_an_instruction_has_no_CSharp()
    {
        var document = CircleGrow(DecompilationLanguage.ILWithCSharp);

        Assert.Contains(document.Lines, l => l.Text.Trim() == "// (no C# code)");
        Assert.DoesNotContain(document.Lines, l => l.Text.Contains("double grown = Radius;", StringComparison.Ordinal) && !l.Text.TrimStart().StartsWith("//"));
    }

    private static DecompiledDocument CircleGrow(DecompilationLanguage language)
    {
        var circle = Trees.TestAssembly.Fixtures().Child("Circle");
        var grow = circle.Children.Single(c => c.Text.StartsWith("Grow", StringComparison.Ordinal));
        return grow.Decompile(language);
    }

    [Fact]
    public void Xml_documentation_sits_above_its_member_as_a_comment()
    {
        var document = Trees.CalculatorDocument(DecompilationLanguage.CSharp);

        const string comment = "/// <summary>How many items were added.</summary>";
        var summary = document.Lines[document.LineContaining("public int Count =>") - 1];

        Assert.Equal(comment, summary.Text.Trim());
        Assert.Equal(SpanKind.Comment, summary.SpanAt(comment).Kind);
    }

    [Fact]
    public void Xml_documentation_over_several_lines_folds_collapsed_to_its_first_line()
    {
        var document = Trees.CalculatorDocument(DecompilationLanguage.CSharp);

        var docs = document.FoldAt("/// <summary>Adds an item");

        Assert.True(docs.DefaultCollapsed);
        Assert.Equal("…", docs.CollapsedText);
        Assert.Contains("<param name=\"item\">", document.Lines[docs.EndLine].Text);
        Assert.Contains("public void Add(T item)", document.Lines[docs.EndLine + 1].Text);
    }

    [Fact]
    public void Turning_xml_documentation_off_leaves_it_out()
    {
        var count = Trees.TestAssembly.Calculator().Child("Count : int");
        var docs = DecompilerToggle.Find("ShowXmlDocumentation")!;
        var off = DecompilerOptions.Default.With(docs, false);

        Assert.Contains("/// <summary>How many items were added.</summary>", count.Decompile(DecompilationLanguage.CSharp).GetText());
        Assert.DoesNotContain("///", count.Decompile(DecompilationLanguage.CSharp, off).GetText());
    }

    [Fact]
    public void Runtime_assemblies_take_their_xml_documentation_from_the_reference_pack()
    {
        var coreLib = new AssemblyList().Open(Trees.CoreLibPath);

        var empty = coreLib.Root.Child("System", "String", "Empty : string").Decompile(DecompilationLanguage.CSharp);

        Assert.True(empty.LineContaining("/// <summary>") < empty.LineContaining("public static readonly string Empty;"));
    }

    [Fact]
    public void A_toggle_changes_the_code_until_the_version_is_too_old_for_it_and_keeps_its_state()
    {
        var assembly = Trees.TestAssembly;
        var count = assembly.Calculator().Child("Count : int");
        var bodies = DecompilerToggle.Find("UseExpressionBodyForCalculatedGetterOnlyProperties")!;
        var off = DecompilerOptions.Default.With(bodies, false);
        var offOnCSharp5 = off with { LanguageVersion = CSharpVersion.CSharp5 };

        Assert.Contains("return _items.Count;", count.Decompile(DecompilationLanguage.CSharp, off).GetText());
        Assert.False(offOnCSharp5.Allows(bodies));
        Assert.False(offOnCSharp5.IsOn(bodies));
        Assert.True(DecompilerOptions.Default.IsOn(bodies));
        Assert.Equal(DecompilerOptions.Default, off.With(bodies, true));
        Assert.Equal(off, DecompilerOptions.Default.With(bodies, false));
    }

    [Fact]
    public void IL_shows_opcodes_labels_and_directives()
    {
        var assembly = Trees.TestAssembly;

        var document = assembly.Calculator().Child("Describe() : string").Decompile(DecompilationLanguage.IL);

        Assert.Equal(DecompilationLanguage.IL, document.Language);
        var spans = document.SpansOf().ToArray();
        Assert.Contains(spans, s => s.Span.Kind == SpanKind.ILOpcode && s.Text == "ret");
        Assert.Contains(spans, s => s.Span.Kind == SpanKind.ILLabel && s.Text == "IL_0000" && s.Span.IsDefinition);
        Assert.Contains(spans, s => s.Span.Kind == SpanKind.Keyword && s.Text == ".maxstack");
        Assert.Contains(spans, s => s.Span.Kind == SpanKind.String && s.Text == "\"Total: \"");
        Assert.Contains(spans, s => s.Span.Kind == SpanKind.Comment && s.Text.StartsWith("// Method begins"));
        Assert.Contains(spans, s => s.Span.Kind == SpanKind.Method && s.Text == "get_Count" && s.Span.Reference is not null);
    }

    [Fact]
    public void The_assembly_node_shows_a_summary_and_assembly_attributes()
    {
        var assembly = Trees.TestAssembly;

        var csharp = assembly.Root.Decompile(DecompilationLanguage.CSharp).GetText();
        var il = assembly.Root.Decompile(DecompilationLanguage.IL).GetText();

        Assert.Contains($"// {Trees.TestAssemblyPath}", csharp);
        Assert.Contains("[assembly: AssemblyProduct(\"ZGF.ILSpy.Tests\")]", csharp);
        Assert.Contains(".assembly extern System.Runtime", il);
        Assert.Contains(".assembly ZGF.ILSpy.Tests", il);
    }

    [Fact]
    public void A_namespace_lists_its_types_as_links_to_them()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var fixtures = assembly.Fixtures();

        var document = fixtures.Decompile(DecompilationLanguage.CSharp);
        var line = document.Lines[document.LineContaining("internal sealed class Circle")];
        var circle = line.SpanAt("Circle");

        Assert.Equal("namespace ZGF.ILSpy.Tests.Fixtures", document.Lines[0].Text);
        Assert.False(circle.IsDefinition);
        Assert.Same(fixtures.Child("Circle"), list.FindNode(circle.Reference!));
    }

    [Fact]
    public void Folders_and_resources_decompile_to_summaries()
    {
        var coreLib = new AssemblyList().Open(Trees.CoreLibPath);

        var references = Trees.TestAssembly.Root.Child("References").Decompile(DecompilationLanguage.CSharp);
        var resources = coreLib.Root.Child("Resources");
        var strings = resources.Children.First(r => r.Text.EndsWith(".resources")).Decompile(DecompilationLanguage.CSharp);

        Assert.Contains(references.SpansOf(), s => s.Text.StartsWith("System.Runtime, Version=") && s.Span.Reference?.Kind == CodeReferenceKind.Assembly);
        Assert.False(strings.IsError);
        Assert.Contains(strings.SpansOf(), s => s.Span.Kind == SpanKind.String);
    }

    [Fact]
    public void Cancellation_throws_instead_of_returning_a_document()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.Throws<OperationCanceledException>(() => assembly.Calculator().Decompile(DecompilationLanguage.CSharp, cancelled.Token));
        Assert.ThrowsAny<OperationCanceledException>(() =>
            NodeContentLoader.LoadAsync(assembly.Calculator(), DecompilationLanguage.IL, DecompilerOptions.Default, cancelled.Token).GetAwaiter().GetResult());
    }

    [Fact]
    public async Task Concurrent_decompiles_match_sequential_ones()
    {
        var list = new AssemblyList();
        var coreLib = list.Open(Trees.CoreLibPath);
        var types = coreLib.Root.Child("System.Collections.Generic").Children.Take(12).ToArray();
        var languages = new[] { DecompilationLanguage.CSharp, DecompilationLanguage.IL };

        var sequential = types.SelectMany(t => languages.Select(l => t.Decompile(l).GetText())).ToArray();
        var concurrent = (await Task.WhenAll(types.SelectMany(t => languages.Select(l => NodeContentLoader.LoadAsync(t, l, DecompilerOptions.Default)))))
            .Select(c => Assert.IsType<DocumentContent>(c).Document).ToArray();

        Assert.Equal(sequential, concurrent.Select(d => d.GetText()));
        Assert.DoesNotContain(concurrent, d => d.IsError);
    }

    [Fact]
    public void Failures_become_an_error_document()
    {
        using var temp = new TempDir();
        var corrupt = temp.Write("corrupt.dll", WithBrokenAttribute(File.ReadAllBytes(Trees.TestAssemblyPath), "Describe"));
        var describe = new AssemblyList().Open(corrupt).Calculator().Child("Describe() : string");

        var il = describe.Decompile(DecompilationLanguage.IL);
        var csharp = describe.Decompile(DecompilationLanguage.CSharp);

        Assert.True(il.IsError);
        Assert.Equal("Describe() : string", il.Title);
        Assert.StartsWith("// Error decompiling", il.Lines[0].Text);
        Assert.Contains(il.SpansOf(), s => s.Text.Contains(nameof(BadImageFormatException)));
        Assert.False(csharp.IsError);
        Assert.Contains("Could not decode attribute arguments", csharp.GetText());
    }

    [Fact]
    public void Every_node_without_code_is_summarized_in_both_languages()
    {
        using var temp = new TempDir();
        var package = temp.Write("Sample.1.0.0.nupkg", []);
        using (var archive = ZipFile.Open(package, ZipArchiveMode.Update))
        {
            archive.CreateEntryFromFile(Trees.TestAssemblyPath, "lib/net10.0/ZGF.ILSpy.Tests.dll");
            using var writer = new StreamWriter(archive.CreateEntry("readme.txt").Open());
            writer.Write("Read me");
        }
        var list = new AssemblyList();
        var roots = new[] { Trees.TestAssemblyPath, Trees.SampleAppPath, package }
            .Select(p => list.Open(p).Root);

        var failed = ListSummarized(roots)
            .SelectMany(static n => new[] { DecompilationLanguage.CSharp, DecompilationLanguage.IL }.Select(l => (Node: n, Document: n.Decompile(l))))
            .Where(static d => d.Document.IsError)
            .Select(static d => $"{d.Node.GetType().Name} {d.Node.Text}")
            .ToList();

        Assert.Empty(failed);
    }

    /// <summary>The nodes under <paramref name="roots"/> that show no type's or member's code, not
    /// following members, base and derived types, or references into what they name.</summary>
    private static List<BrowseNode> ListSummarized(IEnumerable<BrowseNode> roots)
    {
        var nodes = new List<BrowseNode>();
        var pending = new Stack<BrowseNode>(roots);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is not EntityNode and not AssemblyNode and not ErrorNode)
                nodes.Add(node);
            if (node is MemberNode or BaseTypeNode or DerivedTypeNode or AssemblyReferenceNode)
                continue;
            foreach (var child in node.Children)
                pending.Push(child);
        }
        return nodes;
    }

    private static byte[] WithBrokenAttribute(byte[] image, string methodName)
    {
        using var pe = new PEReader(new MemoryStream(image));
        var reader = pe.GetMetadataReader();
        var method = reader.MethodDefinitions.First(h => reader.GetString(reader.GetMethodDefinition(h).Name) == methodName);
        var attribute = reader.GetMethodDefinition(method).GetCustomAttributes().Single();
        var rowSize = reader.GetTableRowSize(TableIndex.CustomAttribute);
        var blobIndexSize = reader.GetHeapSize(HeapIndex.Blob) < 0x10000 ? 2 : 4;
        var offset = pe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.CustomAttribute) +
                     rowSize * (MetadataTokens.GetRowNumber(attribute) - 1) + rowSize - blobIndexSize;
        image.AsSpan(offset, blobIndexSize).Fill(0xFF);
        return image;
    }
}
