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
        var (_, assembly) = Trees.OpenTestAssembly();

        var document = assembly.Calculator().Decompile(DecompilationLanguage.CSharp);

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
        var (_, assembly) = Trees.OpenTestAssembly();

        foreach (var language in new[] { DecompilationLanguage.CSharp, DecompilationLanguage.IL })
        {
            var document = assembly.Calculator().Decompile(language);
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
        var (_, assembly) = Trees.OpenTestAssembly();

        var document = assembly.Calculator().Child("Describe() : string").Decompile(DecompilationLanguage.CSharp);

        Assert.Equal("[Description(\"Summarizes the calculator\")]", document.Lines[document.LineContaining("internal string Describe()") - 1].Text);
        Assert.DoesNotContain(document.Lines.ToArray(), l => l.Text.Contains("class"));
    }

    [Fact]
    public void An_older_language_version_avoids_newer_syntax()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
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
    public void IL_shows_opcodes_labels_and_directives()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

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
        var (_, assembly) = Trees.OpenTestAssembly();

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
        var (_, assembly) = Trees.OpenTestAssembly();
        var coreLib = new AssemblyList().Open(Trees.CoreLibPath);

        var references = assembly.Root.Child("References").Decompile(DecompilationLanguage.CSharp);
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
            assembly.Calculator().DecompileAsync(DecompilationLanguage.IL, DecompilerOptions.Default, cancelled.Token).GetAwaiter().GetResult());
    }

    [Fact]
    public async Task Concurrent_decompiles_match_sequential_ones()
    {
        var list = new AssemblyList();
        var coreLib = list.Open(Trees.CoreLibPath);
        var types = coreLib.Root.Child("System.Collections.Generic").Children.Take(12).ToArray();
        var languages = new[] { DecompilationLanguage.CSharp, DecompilationLanguage.IL };

        var sequential = types.SelectMany(t => languages.Select(l => t.Decompile(l).GetText())).ToArray();
        var concurrent = await Task.WhenAll(types.SelectMany(t => languages.Select(l => t.DecompileAsync(l, DecompilerOptions.Default))));

        Assert.Equal(sequential, concurrent.Select(d => d.GetText()));
        Assert.DoesNotContain(concurrent, d => d.IsError);
    }

    [Fact]
    public void Failures_become_an_error_document()
    {
        var corrupt = Path.Combine(Path.GetTempPath(), $"zgf-ilspy-{Guid.NewGuid():N}.dll");
        File.WriteAllBytes(corrupt, WithBrokenAttribute(File.ReadAllBytes(Trees.TestAssemblyPath), "Describe"));
        try
        {
            var assembly = new AssemblyList().Open(corrupt);
            var describe = assembly.Calculator().Child("Describe() : string");

            var il = describe.Decompile(DecompilationLanguage.IL);
            var csharp = describe.Decompile(DecompilationLanguage.CSharp);

            Assert.True(il.IsError);
            Assert.Equal("Describe() : string", il.Title);
            Assert.StartsWith("// Error decompiling", il.Lines[0].Text);
            Assert.Contains(il.SpansOf(), s => s.Text.Contains(nameof(BadImageFormatException)));
            Assert.False(csharp.IsError);
            Assert.Contains("Could not decode attribute arguments", csharp.GetText());
        }
        finally
        {
            File.Delete(corrupt);
        }
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
