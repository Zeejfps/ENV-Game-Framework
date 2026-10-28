using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class AnchorTests
{
    [Fact]
    public void A_statement_is_found_again_in_the_methods_own_document_and_in_IL()
    {
        var type = Trees.CalculatorDocument(DecompilationLanguage.CSharp);
        var line = type.LineContaining("Changed?.Invoke(this, EventArgs.Empty);");

        var anchor = type.AnchorAt(line)!.Value;
        var method = Trees.TestAssembly.Calculator().Child("Add(T) : void").Decompile(DecompilationLanguage.CSharp);
        var il = Trees.CalculatorDocument(DecompilationLanguage.IL);
        var ilLine = il.FindLine(anchor);

        Assert.Equal(CodeAnchorKind.Statement, anchor.Kind);
        Assert.Equal(line, type.FindLine(anchor));
        Assert.Contains("Changed?.Invoke", method.Lines[method.FindLine(anchor)].Text);
        Assert.StartsWith($"IL_{anchor.ILOffset:x4}", il.Lines[ilLine].Text.TrimStart());
        Assert.Equal(anchor, il.AnchorAt(ilLine));
    }

    [Fact]
    public void A_definition_is_found_again_in_either_language()
    {
        var type = Trees.CalculatorDocument(DecompilationLanguage.CSharp);
        var line = type.LineContaining("public void Add(T item)");

        var anchor = type.AnchorAt(line)!.Value;
        var il = Trees.CalculatorDocument(DecompilationLanguage.IL);

        Assert.Equal(CodeAnchorKind.Definition, anchor.Kind);
        Assert.Equal(line, type.FindLine(anchor));
        Assert.Equal(il.FindDefinitionLine(Trees.TestAssembly.Calculator().Child("Add(T) : void")), il.FindLine(anchor));
    }

    [Theory]
    [InlineData("Changed?.Invoke(this, EventArgs.Empty);")]
    [InlineData("public void Add(T item)")]
    public void An_anchor_finds_its_line_whatever_the_case_of_its_path(string text)
    {
        var type = Trees.CalculatorDocument(DecompilationLanguage.CSharp);
        var line = type.LineContaining(text);
        var anchor = type.AnchorAt(line)!.Value;

        var upper = anchor with { FilePath = anchor.FilePath.ToUpperInvariant() };

        Assert.Equal(anchor, upper);
        Assert.Equal(anchor.GetHashCode(), upper.GetHashCode());
        Assert.Equal(line, type.FindLine(upper));
    }

    [Fact]
    public void A_type_anchor_is_the_same_whatever_the_case_of_its_path_but_not_of_its_name()
    {
        var anchor = new TypeAnchor(@"C:\Lib\A.dll", "N.Outer+Inner`1");

        Assert.Equal(anchor, anchor with { AssemblyPath = @"c:\lib\a.DLL" });
        Assert.Equal(anchor.GetHashCode(), (anchor with { AssemblyPath = @"c:\lib\a.DLL" }).GetHashCode());
        Assert.NotEqual(anchor, anchor with { FullName = "n.outer+inner`1" });
    }

    [Fact]
    public void Anchors_looked_up_from_many_threads_at_once_all_find_their_lines()
    {
        var first = Trees.CalculatorDocument(DecompilationLanguage.CSharp);
        var lines = new[] { first.LineContaining("Changed?.Invoke(this, EventArgs.Empty);"), first.LineContaining("public void Add(T item)") };
        var anchors = lines.Select(l => first.AnchorAt(l)!.Value).ToArray();

        for (var round = 0; round < 20; round++)
        {
            var fresh = Trees.TestAssembly.Calculator().Decompile(DecompilationLanguage.CSharp);
            var found = new int[64];
            Parallel.For(0, found.Length, i => found[i] = fresh.FindLine(anchors[i % anchors.Length]));

            Assert.All(Enumerable.Range(0, found.Length), i => Assert.Equal(lines[i % lines.Length], found[i]));
        }
    }

    [Fact]
    public void A_line_with_no_statement_or_definition_has_no_anchor()
    {
        var type = Trees.CalculatorDocument(DecompilationLanguage.CSharp);
        var brace = type.LineContaining("public void Add(T item)") + 1;

        Assert.Equal("{", type.Lines[brace].Text.Trim());
        Assert.Null(type.AnchorAt(brace));
    }

    [Fact]
    public void A_path_rebuilt_from_its_segments_names_the_same_node()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var path = assembly.Calculator().Child("Add(T) : void").Path;

        var rebuilt = NodePath.From(path.FilePath, path.Segments.ToList());

        Assert.Equal(path, rebuilt);
        Assert.Same(assembly.Calculator().Child("Add(T) : void"), list.FindNode(rebuilt));
    }
}
