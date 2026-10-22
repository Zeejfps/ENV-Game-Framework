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
