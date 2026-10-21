using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class FoldingTests
{
    [Fact]
    public void CSharp_folds_the_usings_collapsed_and_type_and_member_bodies_open()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var document = assembly.Calculator().Decompile(DecompilationLanguage.CSharp);

        var usings = document.FoldAt("using System;");
        Assert.Equal(document.LineContaining("using ZGF.ILSpy.Tests.Fixtures;"), usings.EndLine);
        Assert.True(usings.DefaultCollapsed);
        Assert.Equal("…", usings.CollapsedText);

        var type = document.FoldAt("public class Calculator<T>");
        Assert.Equal(document.Lines.Length - 1, type.EndLine);
        Assert.Equal("}", document.Lines[type.EndLine].Text);
        Assert.False(type.DefaultCollapsed);
        Assert.Equal("{ … }", type.CollapsedText);

        var add = document.FoldAt("public void Add(T item)");
        Assert.Equal("{", document.Lines[add.StartLine + 1].Text.Trim());
        Assert.Equal("}", document.Lines[add.EndLine].Text.Trim());
        Assert.Equal(add.StartLine + 4, add.EndLine);
        Assert.True(add.IsDefinition);
        Assert.False(add.DefaultCollapsed);

        var mode = document.FoldAt("public enum Mode");
        Assert.True(mode.StartLine > type.StartLine && mode.EndLine < type.EndLine);
        Assert.Equal(-1, document.FoldStartingAt(document.LineContaining("public int Count =>")));
    }

    [Fact]
    public void IL_folds_types_and_methods_from_their_signatures()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var document = assembly.Calculator().Decompile(DecompilationLanguage.IL);

        var type = document.FoldAt("extends [System.Runtime]System.Object");
        Assert.StartsWith("} // end of class", document.Lines[type.EndLine].Text);
        Assert.Equal("{ … }", type.CollapsedText);

        var mode = document.FoldAt("extends [System.Runtime]System.Enum");
        Assert.StartsWith("} // end of class Mode", document.Lines[mode.EndLine].Text.Trim());
        Assert.True(mode.StartLine > type.StartLine && mode.EndLine < type.EndLine);

        var describe = document.Folds.Single(f => document.Lines[f.EndLine].Text.Contains("end of method Calculator`1::Describe"));
        Assert.Contains("Describe", document.Lines[describe.StartLine].Text);
        Assert.Equal("{", document.Lines[describe.StartLine + 1].Text.Trim());
        Assert.True(describe.IsDefinition);
        Assert.False(describe.DefaultCollapsed);
    }

    [Theory]
    [InlineData(DecompilationLanguage.CSharp)]
    [InlineData(DecompilationLanguage.IL)]
    public void Folds_are_ordered_on_lines_of_their_own_and_nest_whole(DecompilationLanguage language)
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var document = assembly.Calculator().Decompile(language);

        Assert.True(document.Folds.Length >= 5);
        var open = new Stack<FoldRegion>();
        var previous = -1;
        foreach (var fold in document.Folds)
        {
            Assert.True(fold.StartLine > previous);
            Assert.True(fold.EndLine > fold.StartLine);
            Assert.True(fold.EndLine < document.Lines.Length);
            while (open.Count > 0 && open.Peek().EndLine < fold.StartLine)
                open.Pop();
            if (open.Count > 0)
                Assert.True(fold.EndLine <= open.Peek().EndLine);
            open.Push(fold);
            previous = fold.StartLine;
            Assert.Equal(fold, document.Folds[document.FoldStartingAt(fold.StartLine)]);
        }
    }

    [Fact]
    public void A_member_on_its_own_folds_its_body_and_plain_text_folds_nothing()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var document = assembly.Calculator().Child("Describe() : string").Decompile(DecompilationLanguage.CSharp);

        var body = Assert.Single(document.Folds);
        Assert.Equal(document.LineContaining("internal string Describe()"), body.StartLine);
        Assert.Empty(DecompiledDocument.FromText("t", "a\n{\nb\n}").Folds);
    }
}

internal static class FoldQueries
{
    public static FoldRegion FoldAt(this DecompiledDocument document, string text)
    {
        var index = document.FoldStartingAt(document.LineContaining(text));
        Assert.True(index >= 0, $"No fold starts at '{text}'");
        return document.Folds[index];
    }
}
