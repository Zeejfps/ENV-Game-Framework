using ZGF.Markdown;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A markdown block as <see cref="UiWidgets.Markdown"/> draws it: its text already styled
/// into runs, which are kept while the block is unchanged, so their layouts are too. Each text it
/// shows is a leaf, numbered in reading order, which a selection runs across.</summary>
internal abstract record ShownBlock
{
    /// <summary>A paragraph or a heading.</summary>
    public sealed record Text(int Leaf) : ShownBlock;

    /// <summary>A fenced code block, its lines kept as they are and scrolled sideways when wider than
    /// the view.</summary>
    public sealed record Code(string? Language, int Leaf) : ShownBlock;

    public sealed record List(IReadOnlyList<ShownItem> Items) : ShownBlock;

    public sealed record Quote(IReadOnlyList<ShownBlock> Blocks) : ShownBlock;

    public sealed record Break : ShownBlock;

    /// <summary>A table, one cell per column in <see cref="Header"/> and in each of
    /// <see cref="Rows"/>, which the parser may leave short. Its cells are leaves row by row.</summary>
    public sealed record Table(
        IReadOnlyList<ColumnAlignment> Columns,
        IReadOnlyList<int> Header,
        IReadOnlyList<IReadOnlyList<int>> Rows) : ShownBlock;
}

internal sealed record ShownItem(ItemMarker Marker, IReadOnlyList<ShownBlock> Blocks);

/// <summary>What leads a list item: a bullet or number, or a task's check box.</summary>
internal abstract record ItemMarker
{
    public sealed record Label(string Text) : ItemMarker;

    public sealed record Task(bool Checked) : ItemMarker;
}

/// <summary>One text a selection can cover: its styled runs, their joined text, whether it wraps,
/// and what joins it to the leaf before it when both are copied — a tab between the cells of a
/// table row, else a line break.</summary>
internal sealed record MarkdownLeaf(IReadOnlyList<RichTextRun> Runs, string Text, bool Wrap, string Before);

/// <summary>Turns parsed markdown into <see cref="ShownBlock"/>s in the theme's styles, adding their
/// leaves in reading order.</summary>
internal static class ShownMarkdown
{
    private const string Bullet = "•";
    private const string LineBreak = "\n";
    private const string CellBreak = "\t";
    private const float TitleScale = 1.25f;

    /// <summary>The block shown for <paramref name="block"/>, its plain text in
    /// <paramref name="color"/>, its leaves added to <paramref name="leaves"/>.</summary>
    public static ShownBlock Build(Ui ui, MarkdownLook look, MarkdownBlock block, uint color, List<MarkdownLeaf> leaves) => block switch
    {
        ParagraphBlock paragraph => new ShownBlock.Text(AddLeaf(leaves, Runs(ui, look, paragraph.Runs, ui.Theme.FontSize, color, bold: false), wrap: true, LineBreak)),
        HeadingBlock heading => new ShownBlock.Text(AddLeaf(leaves, Runs(ui, look, heading.Runs, HeadingSize(ui.Theme, heading.Level), color, bold: true), wrap: true, LineBreak)),
        CodeBlock code => Code(ui, look, code, leaves),
        ListBlock list => List(ui, look, list, color, leaves),
        QuoteBlock quote => new ShownBlock.Quote(BuildAll(ui, look, quote.Blocks, ui.Theme.TextMuted, leaves)),
        ThematicBreakBlock => new ShownBlock.Break(),
        TableBlock table => Table(ui, look, table, color, leaves),
        _ => throw new InvalidOperationException($"No way to show a {block.GetType().Name}."),
    };

    private static int AddLeaf(List<MarkdownLeaf> leaves, IReadOnlyList<RichTextRun> runs, bool wrap, string before)
    {
        leaves.Add(new MarkdownLeaf(runs, RichTextLayout.ConcatenateRuns(runs, out _), wrap, before));
        return leaves.Count - 1;
    }

    private static IReadOnlyList<ShownBlock> BuildAll(Ui ui, MarkdownLook look, IReadOnlyList<MarkdownBlock> blocks, uint color, List<MarkdownLeaf> leaves)
    {
        var shown = new ShownBlock[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
            shown[i] = Build(ui, look, blocks[i], color, leaves);
        return shown;
    }

    /// <summary>A first-level heading a step over <see cref="UiTheme.FontSizeTitle"/>, a second-level
    /// one at it, and the rest at body size, set apart by bold alone.</summary>
    private static float HeadingSize(UiTheme theme, int level) => level switch
    {
        1 => MathF.Round(theme.FontSizeTitle * TitleScale),
        2 => theme.FontSizeTitle,
        _ => theme.FontSize,
    };

    private static ShownBlock Code(Ui ui, MarkdownLook look, CodeBlock code, List<MarkdownLeaf> leaves)
    {
        var text = code.Text.TrimEnd('\n');
        var style = ui.StyleText(ui.Theme.Text, ui.Theme.FontSize, family: look.CodeFamily);
        return new ShownBlock.Code(code.Language, AddLeaf(leaves, [new RichTextRun(text, style)], wrap: false, LineBreak));
    }

    private static ShownBlock List(Ui ui, MarkdownLook look, ListBlock list, uint color, List<MarkdownLeaf> leaves)
    {
        var items = new ShownItem[list.Items.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var item = list.Items[i];
            items[i] = new ShownItem(MarkerOf(list, item, i), BuildAll(ui, look, item.Blocks, color, leaves));
        }
        return new ShownBlock.List(items);
    }

    private static ItemMarker MarkerOf(ListBlock list, ListItem item, int index)
    {
        if (item.TaskChecked is { } done)
            return new ItemMarker.Task(done);
        if (list.Ordered)
            return new ItemMarker.Label($"{list.Start + index}.");
        return new ItemMarker.Label(Bullet);
    }

    private static ShownBlock Table(Ui ui, MarkdownLook look, TableBlock table, uint color, List<MarkdownLeaf> leaves)
    {
        var header = Cells(ui, look, table.Header, color, bold: true, leaves);
        var rows = new IReadOnlyList<int>[table.Rows.Count];
        for (var r = 0; r < rows.Length; r++)
            rows[r] = Cells(ui, look, table.Rows[r], color, bold: false, leaves);
        return new ShownBlock.Table(table.Columns, header, rows);
    }

    private static IReadOnlyList<int> Cells(Ui ui, MarkdownLook look, IReadOnlyList<IReadOnlyList<InlineRun>> cells, uint color, bool bold,
        List<MarkdownLeaf> leaves)
    {
        var shown = new int[cells.Count];
        for (var c = 0; c < shown.Length; c++)
            shown[c] = AddLeaf(leaves, Runs(ui, look, cells[c], ui.Theme.FontSize, color, bold), wrap: false, c == 0 ? LineBreak : CellBreak);
        return shown;
    }

    /// <summary>Styled runs for parsed inline text: bold, italic in <see cref="MarkdownLook.ItalicFamily"/>,
    /// code in <see cref="MarkdownLook.CodeFamily"/> on a chip, struck through, and links in
    /// <see cref="UiTheme.AccentText"/>, each leading to its URL. A picture amid text shows as its
    /// alt text, or its address when it has none, and leads to it.</summary>
    private static IReadOnlyList<RichTextRun> Runs(Ui ui, MarkdownLook look, IReadOnlyList<InlineRun> runs, float size, uint color, bool bold)
    {
        var shown = new RichTextRun[runs.Count];
        for (var i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            var link = run.LinkUrl ?? run.ImageSrc;
            var text = run.Text.Length == 0 && run.ImageSrc is { } src ? src : run.Text;
            var family = run.Code ? look.CodeFamily : run.Italic ? look.ItalicFamily : null;
            var style = ui.StyleText(link != null ? ui.Theme.AccentText : color, size, bold || run.Bold, family);
            shown[i] = new RichTextRun(text, style, IsCode: run.Code, Strikethrough: run.Strikethrough, Link: link);
        }
        return shown;
    }
}
