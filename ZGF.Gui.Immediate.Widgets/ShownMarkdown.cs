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

/// <summary>
/// One text a selection can cover: its styled runs, their joined text, whether it wraps, and what
/// joins it to the leaf before it when both are copied as text — a tab between the cells of a table
/// row, else a line break.
/// <para>For copying as HTML, the parsed runs it was styled from, one per styled run; the element it
/// is, <see cref="Tag"/>; and the containers it sits in, outermost first, <see cref="Path"/>. Leaves
/// in the same container share the very same instance of it.</para>
/// </summary>
internal sealed record MarkdownLeaf(
    IReadOnlyList<RichTextRun> Runs,
    string Text,
    bool Wrap,
    string Before,
    IReadOnlyList<InlineRun> Source,
    LeafTag Tag,
    IReadOnlyList<HtmlContainer> Path);

/// <summary>The element a leaf is when copied as HTML.</summary>
internal abstract record LeafTag
{
    public sealed record Paragraph : LeafTag;

    public sealed record Heading(int Level) : LeafTag;

    public sealed record Code(string? Language) : LeafTag;

    public sealed record Cell(bool Header) : LeafTag;
}

/// <summary>An element leaves sit in when copied as HTML. Containers are told apart by instance, not
/// value: two list items alike are still two items.</summary>
internal abstract class HtmlContainer
{
    public sealed class List(bool ordered) : HtmlContainer
    {
        public bool Ordered { get; } = ordered;
    }

    /// <summary>A list item and the number it shows in an ordered list.</summary>
    public sealed class Item(int number) : HtmlContainer
    {
        public int Number { get; } = number;
    }

    public sealed class Quote : HtmlContainer;

    public sealed class Table : HtmlContainer;

    public sealed class Row : HtmlContainer;
}

/// <summary>Turns parsed markdown into <see cref="ShownBlock"/>s in the theme's styles, adding their
/// leaves in reading order.</summary>
internal static class ShownMarkdown
{
    private const string Bullet = "•";
    private const string LineBreak = "\n";
    private const string CellBreak = "\t";
    private const float TitleScale = 1.25f;

    /// <summary>The block shown for <paramref name="block"/>, its plain text in
    /// <paramref name="color"/>, its leaves added to <paramref name="leaves"/> inside
    /// <paramref name="path"/>.</summary>
    public static ShownBlock Build(Ui ui, MarkdownLook look, MarkdownBlock block, uint color, List<MarkdownLeaf> leaves,
        IReadOnlyList<HtmlContainer> path) => block switch
    {
        ParagraphBlock paragraph => new ShownBlock.Text(AddLeaf(leaves, Runs(ui, look, paragraph.Runs, ui.Theme.FontSize, color, bold: false),
            paragraph.Runs, new LeafTag.Paragraph(), path)),
        HeadingBlock heading => new ShownBlock.Text(AddLeaf(leaves, Runs(ui, look, heading.Runs, HeadingSize(ui.Theme, heading.Level), color, bold: true),
            heading.Runs, new LeafTag.Heading(heading.Level), path)),
        CodeBlock code => Code(ui, look, code, leaves, path),
        ListBlock list => List(ui, look, list, color, leaves, path),
        QuoteBlock quote => new ShownBlock.Quote(BuildAll(ui, look, quote.Blocks, ui.Theme.TextMuted, leaves, [.. path, new HtmlContainer.Quote()])),
        ThematicBreakBlock => new ShownBlock.Break(),
        TableBlock table => Table(ui, look, table, color, leaves, path),
        _ => throw new InvalidOperationException($"No way to show a {block.GetType().Name}."),
    };

    /// <summary>Adds the leaf of <paramref name="runs"/>, styled one for one from
    /// <paramref name="source"/>; code and table cells keep their lines, the rest wrap.</summary>
    private static int AddLeaf(List<MarkdownLeaf> leaves, IReadOnlyList<RichTextRun> runs, IReadOnlyList<InlineRun> source, LeafTag tag,
        IReadOnlyList<HtmlContainer> path, string before = LineBreak)
    {
        leaves.Add(new MarkdownLeaf(runs, RichTextLayout.ConcatenateRuns(runs, out _), tag is not (LeafTag.Code or LeafTag.Cell), before, source, tag,
            path));
        return leaves.Count - 1;
    }

    private static IReadOnlyList<ShownBlock> BuildAll(Ui ui, MarkdownLook look, IReadOnlyList<MarkdownBlock> blocks, uint color, List<MarkdownLeaf> leaves,
        IReadOnlyList<HtmlContainer> path)
    {
        var shown = new ShownBlock[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
            shown[i] = Build(ui, look, blocks[i], color, leaves, path);
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

    /// <summary>The code, in <see cref="MarkdownLook.CodeFamily"/> as it is, blank lines before the
    /// closing fence included: the parser leaves no line break of the fence's own.</summary>
    private static ShownBlock Code(Ui ui, MarkdownLook look, CodeBlock code, List<MarkdownLeaf> leaves, IReadOnlyList<HtmlContainer> path)
    {
        var text = code.Text;
        var style = ui.StyleText(ui.Theme.Text, ui.Theme.FontSize, family: look.CodeFamily);
        var leaf = AddLeaf(leaves, [new RichTextRun(text, style)], [new InlineRun(text)], new LeafTag.Code(code.Language), path);
        return new ShownBlock.Code(code.Language, leaf);
    }

    private static ShownBlock List(Ui ui, MarkdownLook look, ListBlock list, uint color, List<MarkdownLeaf> leaves, IReadOnlyList<HtmlContainer> path)
    {
        var inList = (IReadOnlyList<HtmlContainer>)[.. path, new HtmlContainer.List(list.Ordered)];
        var items = new ShownItem[list.Items.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var item = list.Items[i];
            var inItem = (IReadOnlyList<HtmlContainer>)[.. inList, new HtmlContainer.Item(list.Start + i)];
            items[i] = new ShownItem(MarkerOf(list, item, i), BuildAll(ui, look, item.Blocks, color, leaves, inItem));
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

    private static ShownBlock Table(Ui ui, MarkdownLook look, TableBlock table, uint color, List<MarkdownLeaf> leaves, IReadOnlyList<HtmlContainer> path)
    {
        var inTable = (IReadOnlyList<HtmlContainer>)[.. path, new HtmlContainer.Table()];
        var header = Cells(ui, look, table.Header, color, header: true, leaves, inTable);
        var rows = new IReadOnlyList<int>[table.Rows.Count];
        for (var r = 0; r < rows.Length; r++)
            rows[r] = Cells(ui, look, table.Rows[r], color, header: false, leaves, inTable);
        return new ShownBlock.Table(table.Columns, header, rows);
    }

    private static IReadOnlyList<int> Cells(Ui ui, MarkdownLook look, IReadOnlyList<IReadOnlyList<InlineRun>> cells, uint color, bool header,
        List<MarkdownLeaf> leaves, IReadOnlyList<HtmlContainer> table)
    {
        var inRow = (IReadOnlyList<HtmlContainer>)[.. table, new HtmlContainer.Row()];
        var shown = new int[cells.Count];
        for (var c = 0; c < shown.Length; c++)
            shown[c] = AddLeaf(leaves, Runs(ui, look, cells[c], ui.Theme.FontSize, color, header), cells[c], new LeafTag.Cell(header), inRow,
                c == 0 ? LineBreak : CellBreak);
        return shown;
    }

    /// <summary>Styled runs for parsed inline text, one for each: bold, italic in
    /// <see cref="MarkdownLook.ItalicFamily"/>, code in <see cref="MarkdownLook.CodeFamily"/> on a
    /// chip, struck through, and links in <see cref="UiTheme.AccentText"/>, each leading to its URL. A
    /// picture amid text shows as its alt text, or its address when it has none, and leads to it.</summary>
    private static IReadOnlyList<RichTextRun> Runs(Ui ui, MarkdownLook look, IReadOnlyList<InlineRun> runs, float size, uint color, bool bold)
    {
        var shown = new RichTextRun[runs.Count];
        for (var i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            var link = run.LinkUrl ?? run.ImageSrc;
            var family = run.Code ? look.CodeFamily : run.Italic ? look.ItalicFamily : null;
            var style = ui.StyleText(link != null ? ui.Theme.AccentText : color, size, bold || run.Bold, family);
            shown[i] = new RichTextRun(ShownText(run), style, IsCode: run.Code, Strikethrough: run.Strikethrough, Link: link);
        }
        return shown;
    }

    /// <summary>The text a parsed run shows: its own, or for a picture without alt text its address.</summary>
    public static string ShownText(InlineRun run) => run.Text.Length == 0 && run.ImageSrc is { } src ? src : run.Text;
}
