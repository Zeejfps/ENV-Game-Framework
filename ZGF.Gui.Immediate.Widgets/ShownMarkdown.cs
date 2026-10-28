using ZGF.Markdown;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A markdown block as <see cref="UiWidgets.Markdown"/> draws it: its text already styled
/// into runs, which are kept while the block is unchanged, so their layouts are too.</summary>
internal abstract record ShownBlock
{
    /// <summary>A paragraph or a heading.</summary>
    public sealed record Text(IReadOnlyList<RichTextRun> Runs) : ShownBlock;

    /// <summary>A fenced code block, its lines kept as they are and scrolled sideways when wider than
    /// the view.</summary>
    public sealed record Code(string? Language, IReadOnlyList<RichTextRun> Runs) : ShownBlock;

    public sealed record List(IReadOnlyList<ShownItem> Items) : ShownBlock;

    public sealed record Quote(IReadOnlyList<ShownBlock> Blocks) : ShownBlock;

    public sealed record Break : ShownBlock;

    /// <summary>A table, one cell per column in <see cref="Header"/> and in each of
    /// <see cref="Rows"/>, which the parser may leave short.</summary>
    public sealed record Table(
        IReadOnlyList<ColumnAlignment> Columns,
        IReadOnlyList<IReadOnlyList<RichTextRun>> Header,
        IReadOnlyList<IReadOnlyList<IReadOnlyList<RichTextRun>>> Rows) : ShownBlock;
}

internal sealed record ShownItem(ItemMarker Marker, IReadOnlyList<ShownBlock> Blocks);

/// <summary>What leads a list item: a bullet or number, or a task's check box.</summary>
internal abstract record ItemMarker
{
    public sealed record Label(string Text) : ItemMarker;

    public sealed record Task(bool Checked) : ItemMarker;
}

/// <summary>Turns parsed markdown into <see cref="ShownBlock"/>s in the theme's styles.</summary>
internal static class ShownMarkdown
{
    private const string Bullet = "•";
    private const float TitleScale = 1.25f;

    /// <summary>The block shown for <paramref name="block"/>, its plain text in
    /// <paramref name="color"/>.</summary>
    public static ShownBlock Build(Ui ui, MarkdownFonts fonts, MarkdownBlock block, uint color) => block switch
    {
        ParagraphBlock paragraph => new ShownBlock.Text(Runs(ui, fonts, paragraph.Runs, ui.Theme.FontSize, color, bold: false)),
        HeadingBlock heading => new ShownBlock.Text(Runs(ui, fonts, heading.Runs, HeadingSize(ui.Theme, heading.Level), color, bold: true)),
        CodeBlock code => Code(ui, fonts, code),
        ListBlock list => List(ui, fonts, list, color),
        QuoteBlock quote => new ShownBlock.Quote(BuildAll(ui, fonts, quote.Blocks, ui.Theme.TextMuted)),
        ThematicBreakBlock => new ShownBlock.Break(),
        TableBlock table => Table(ui, fonts, table, color),
        _ => throw new InvalidOperationException($"No way to show a {block.GetType().Name}."),
    };

    private static IReadOnlyList<ShownBlock> BuildAll(Ui ui, MarkdownFonts fonts, IReadOnlyList<MarkdownBlock> blocks, uint color)
    {
        var shown = new ShownBlock[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
            shown[i] = Build(ui, fonts, blocks[i], color);
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

    private static ShownBlock Code(Ui ui, MarkdownFonts fonts, CodeBlock code)
    {
        var text = code.Text.TrimEnd('\n');
        var style = ui.StyleText(ui.Theme.Text, ui.Theme.FontSize, family: fonts.Code);
        return new ShownBlock.Code(code.Language, [new RichTextRun(text, style)]);
    }

    private static ShownBlock List(Ui ui, MarkdownFonts fonts, ListBlock list, uint color)
    {
        var items = new ShownItem[list.Items.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var item = list.Items[i];
            items[i] = new ShownItem(MarkerOf(list, item, i), BuildAll(ui, fonts, item.Blocks, color));
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

    private static ShownBlock Table(Ui ui, MarkdownFonts fonts, TableBlock table, uint color)
    {
        var size = ui.Theme.FontSize;
        var header = new IReadOnlyList<RichTextRun>[table.Header.Count];
        for (var i = 0; i < header.Length; i++)
            header[i] = Runs(ui, fonts, table.Header[i], size, color, bold: true);
        var rows = new IReadOnlyList<IReadOnlyList<RichTextRun>>[table.Rows.Count];
        for (var r = 0; r < rows.Length; r++)
        {
            var cells = new IReadOnlyList<RichTextRun>[table.Rows[r].Count];
            for (var c = 0; c < cells.Length; c++)
                cells[c] = Runs(ui, fonts, table.Rows[r][c], size, color, bold: false);
            rows[r] = cells;
        }
        return new ShownBlock.Table(table.Columns, header, rows);
    }

    /// <summary>Styled runs for parsed inline text: bold, italic in <see cref="MarkdownFonts.Italic"/>,
    /// code in <see cref="MarkdownFonts.Code"/> on a chip, struck through, and links in
    /// <see cref="UiTheme.AccentText"/>, each leading to its URL. A picture amid text shows as its
    /// alt text, or its address when it has none, and leads to it.</summary>
    private static IReadOnlyList<RichTextRun> Runs(Ui ui, MarkdownFonts fonts, IReadOnlyList<InlineRun> runs, float size, uint color, bool bold)
    {
        var shown = new RichTextRun[runs.Count];
        for (var i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            var link = run.LinkUrl ?? run.ImageSrc;
            var text = run.Text.Length == 0 && run.ImageSrc is { } src ? src : run.Text;
            var family = run.Code ? fonts.Code : run.Italic ? fonts.Italic : null;
            var style = ui.StyleText(link != null ? ui.Theme.AccentText : color, size, bold || run.Bold, family);
            shown[i] = new RichTextRun(text, style, IsCode: run.Code, Strikethrough: run.Strikethrough, Link: link);
        }
        return shown;
    }
}
