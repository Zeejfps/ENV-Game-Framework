using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Tests;

internal static class NodeQueries
{
    /// <summary>The node at <paramref name="path"/> under <paramref name="node"/>, one label a level.</summary>
    public static BrowseNode Child(this BrowseNode node, params ReadOnlySpan<string> path)
    {
        foreach (var text in path)
            node = ChildNamed(node, text);
        return node;
    }

    /// <summary>The child labelled <paramref name="text"/>; the one the source declared when the
    /// compiler made up another of the same label, as it does for an event's field.</summary>
    private static BrowseNode ChildNamed(BrowseNode node, string text)
    {
        var named = node.Children
            .Where(c => c.Text == text)
            .ToList();
        if (named.Count > 1)
            named = named.FindAll(IsSourceDeclared);
        var child = named.SingleOrDefault();
        if (child == null)
            throw new InvalidOperationException($"'{node.Text}' has no child '{text}': {string.Join(", ", node.Children.Select(c => c.Text))}");
        return child;
    }

    private static bool IsSourceDeclared(BrowseNode node) => !node.IsCompilerGenerated;

    public static NodeContent Content(this BrowseNode node, DecompilationLanguage language = DecompilationLanguage.CSharp) =>
        NodeContentLoader.LoadAsync(TestSession.Shared, node, language, DecompilerOptions.Default).GetAwaiter().GetResult();

    public static DecompiledDocument Decompile(this BrowseNode node, DecompilationLanguage language, CancellationToken cancellationToken = default) =>
        NodeContentLoader.Decompile(TestSession.Shared, node, language, DecompilerOptions.Default, cancellationToken);

    public static DecompiledDocument Decompile(this BrowseNode node, DecompilationLanguage language, DecompilerOptions options,
        CancellationToken cancellationToken = default) =>
        NodeContentLoader.Decompile(TestSession.Shared, node, language, options, cancellationToken);
}

internal static class DocumentQueries
{
    public static (int Line, int Column) Find(this DecompiledDocument document, string text, int skip = 0)
    {
        for (var i = 0; i < document.Lines.Length; i++)
        {
            var at = document.Lines[i].Text.IndexOf(text, StringComparison.Ordinal);
            if (at < 0) continue;
            if (skip-- == 0) return (i, at);
        }
        throw new InvalidOperationException($"'{text}' is not in the document:\n{document.GetText()}");
    }

    public static int LineContaining(this DecompiledDocument document, string text) => document.Find(text).Line;

    /// <summary>The span over <paramref name="column"/> of line <paramref name="line"/>.</summary>
    public static DocumentSpan SpanAt(this DecompiledDocument document, int line, int column)
    {
        foreach (var span in document.Lines[line].Spans)
        {
            if (span.Start <= column && column < span.End) return span;
        }
        throw new InvalidOperationException("No span there.");
    }

    /// <summary>The span that is exactly the first <paramref name="token"/> of <paramref name="line"/>.</summary>
    public static DocumentSpan SpanAt(this DocumentLine line, string token)
    {
        var start = line.Text.IndexOf(token, StringComparison.Ordinal);
        foreach (var span in line.Spans)
        {
            if (span.Start == start && span.Length == token.Length) return span;
        }
        throw new InvalidOperationException($"No span '{token}' in '{line.Text}'");
    }

    public static IEnumerable<(string Text, DocumentSpan Span)> SpansOf(this DecompiledDocument document)
    {
        foreach (var line in document.Lines)
        {
            var text = line.Text;
            var spans = line.Spans.ToArray();
            foreach (var span in spans)
                yield return (text.Substring(span.Start, span.Length), span);
        }
    }

    /// <summary>The index in <see cref="DecompiledDocument.Folds"/> of the fold starting on the line containing <paramref name="text"/>.</summary>
    public static int FoldIndexAt(this DecompiledDocument document, string text)
    {
        var line = document.LineContaining(text);
        var index = document.FoldStartingAt(line);
        if (index < 0)
            throw new InvalidOperationException($"No fold starts at '{text}'");
        return index;
    }

    public static FoldRegion FoldAt(this DecompiledDocument document, string text) => document.Folds[document.FoldIndexAt(text)];
}

internal static class MetadataQueries
{
    /// <summary>The table named <paramref name="name"/> in a metadata tables folder.</summary>
    public static BrowseNode Table(this BrowseNode tables, string name) => tables.Children.Single(c => c.Text.Split(' ')[1] == name);

    public static int Column(this MetadataGrid grid, string name)
    {
        var names = grid.Columns.Select(c => c.Name).ToList();
        return names.IndexOf(name);
    }

    public static int RowNamed(this MetadataGrid grid, string name)
    {
        var column = grid.Column("Name");
        for (var row = 0; row < grid.RowCount; row++)
        {
            if (grid.Text(row, column) == name) return row;
        }
        throw new InvalidOperationException($"No row named {name}");
    }
}
