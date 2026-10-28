using ZGF.Markdown;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>The fonts <see cref="UiWidgets.Markdown"/> draws code and italic text in: families the app
/// registered, or null for the theme's font, which leaves italic text upright.</summary>
public readonly record struct MarkdownFonts(string? Code = null, string? Italic = null);

public static partial class UiWidgets
{
    private const int MarkdownSalt = 0x4D44574E;
    private const float QuoteBarWidth = 3f;

    /// <summary>
    /// <paramref name="text"/> as markdown, filling the width it is given: paragraphs wrapped,
    /// headings, fenced code on a panel that scrolls sideways, bulleted, numbered and task lists,
    /// quotes, rules and tables. Unsupported syntax shows as the text it is. Bold, italic, code,
    /// strikethrough and links style the text within a block; a link underlines under the pointer
    /// and a left or middle click on it is returned, its <see cref="RichTextClick.Link"/> the URL.
    /// The text is parsed again only when it changes, and blocks it still begins with keep their
    /// layouts, so a reply streaming in costs the blocks that grew.
    /// </summary>
    public static RichTextClick Markdown(this Ui ui, string id, string text, MarkdownFonts fonts = default)
    {
        using var scope = ui.Id(id);
        var blocks = ShownBlocksOf(ui, ui.GetId(MarkdownSalt), text, fonts);
        var markdown = new MarkdownDrawing(ui);
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = ui.Theme.Gap }))
            markdown.Blocks(blocks);
        return markdown.Clicked;
    }

    private sealed class MarkdownState
    {
        public string? Text;
        public UiTheme? Theme;
        public MarkdownFonts Fonts;
        public IReadOnlyList<MarkdownBlock> Sources = [];
        public ShownBlock[] Shown = [];
    }

    /// <summary>The blocks <paramref name="text"/> shows, parsed again when it changed since the last
    /// frame, keeping the blocks it still begins with; and built again in full when the theme or the
    /// fonts changed.</summary>
    private static IReadOnlyList<ShownBlock> ShownBlocksOf(Ui ui, int stateId, string text, MarkdownFonts fonts)
    {
        var state = ui.GetState<MarkdownState>(stateId);
        var sameLook = ui.Theme.Equals(state.Theme) && state.Fonts == fonts;
        if (sameLook && state.Text == text)
            return state.Shown;
        var keptCount = sameLook ? state.Sources.Count : 0;
        var parsed = new BasicMarkdownParser().Parse(text).Blocks;
        var shown = new ShownBlock[parsed.Count];
        for (var i = 0; i < parsed.Count; i++)
        {
            if (i < keptCount && state.Sources[i].Equals(parsed[i]))
                shown[i] = state.Shown[i];
            else
                shown[i] = ShownMarkdown.Build(ui, fonts, parsed[i], ui.Theme.Text);
        }
        state.Text = text;
        state.Theme = ui.Theme;
        state.Fonts = fonts;
        state.Sources = parsed;
        state.Shown = shown;
        return shown;
    }

    /// <summary>Records shown blocks, numbering the elements that take input in the order they are
    /// recorded, and keeps the link clicked among them.</summary>
    private sealed class MarkdownDrawing(Ui ui)
    {
        private int _nextId;

        public RichTextClick Clicked { get; private set; }

        private uint CodeChip => ui.Theme.Control;

        public void Blocks(IReadOnlyList<ShownBlock> blocks)
        {
            foreach (var block in blocks)
                Block(block);
        }

        private int NextId() => ui.GetId(_nextId++);

        private void Block(ShownBlock block)
        {
            switch (block)
            {
                case ShownBlock.Text text:
                    Text(text.Runs, wrap: true);
                    break;
                case ShownBlock.Code code:
                    Code(code);
                    break;
                case ShownBlock.List list:
                    List(list);
                    break;
                case ShownBlock.Quote quote:
                    Quote(quote);
                    break;
                case ShownBlock.Break:
                    ui.Separator();
                    break;
                case ShownBlock.Table table:
                    Table(table);
                    break;
                default:
                    throw new InvalidOperationException($"No way to draw a {block.GetType().Name}.");
            }
        }

        private void Text(IReadOnlyList<RichTextRun> runs, bool wrap)
        {
            var click = ui.RichText(runs, codeBackground: CodeChip, wrap: wrap, id: NextId());
            if (click.Link != null)
                Clicked = click;
        }

        /// <summary>The code on a bordered panel, under its language when the fence names one.</summary>
        private void Code(ShownBlock.Code code)
        {
            var theme = ui.Theme;
            using (ui.Column(new Box
            {
                Width = Sizing.Grow(),
                Padding = new Spacing(theme.PanelPadding, theme.Gap),
                Gap = theme.GapTight,
                Background = theme.Surface,
                BorderColor = theme.Border,
                BorderWidth = theme.BorderWidth,
                Radius = theme.PanelRadius,
            }))
            {
                if (code.Language != null)
                    ui.Text(code.Language, color: theme.TextMuted, size: theme.FontSizeSmall);
                using (ui.Scroll(NextId(), ScrollAxes.Horizontal, new Box { Gap = 0f, AlignY = Align.Start }))
                    Text(code.Runs, wrap: false);
            }
        }

        /// <summary>Each item behind its marker, the markers of a list as wide as its widest, so the
        /// items line up.</summary>
        private void List(ShownBlock.List list)
        {
            var theme = ui.Theme;
            var markerWidth = MarkerWidth(list);
            using (ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.GapTight }))
            {
                foreach (var item in list.Items)
                {
                    using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Start }))
                    {
                        Marker(item.Marker, markerWidth);
                        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.GapTight }))
                            Blocks(item.Blocks);
                    }
                }
            }
        }

        private float MarkerWidth(ShownBlock.List list)
        {
            var width = ui.Theme.CheckboxSize;
            foreach (var item in list.Items)
            {
                if (item.Marker is ItemMarker.Label label)
                    width = MathF.Max(width, ui.MeasureText(label.Text));
            }
            return MathF.Ceiling(width);
        }

        private void Marker(ItemMarker marker, float width)
        {
            var theme = ui.Theme;
            switch (marker)
            {
                case ItemMarker.Label label:
                    ui.Text(label.Text, new Box { Width = width }, color: theme.TextMuted, align: Align.End);
                    break;
                case ItemMarker.Task task:
                    using (ui.Row(new Box { Width = width, AlignX = Align.End }))
                    using (ui.Box(new Box
                    {
                        Width = theme.CheckboxSize,
                        Height = theme.CheckboxSize,
                        BorderColor = theme.Border,
                        BorderWidth = theme.BorderWidth,
                        Radius = theme.RadiusSmall,
                    }))
                    {
                        if (task.Checked)
                            ui.PaintGlyph(UiGlyph.Checkmark, theme.Text);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"No way to draw a {marker.GetType().Name}.");
            }
        }

        /// <summary>The quoted blocks behind a bar as tall as they are.</summary>
        private void Quote(ShownBlock.Quote quote)
        {
            var theme = ui.Theme;
            using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.Gap, AlignY = Align.Start }))
            {
                ui.Leaf(new Box { Width = QuoteBarWidth, Height = Sizing.Grow(), Background = theme.Border });
                using (ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.Gap }))
                    Blocks(quote.Blocks);
            }
        }

        /// <summary>The table column by column, each as wide as its widest cell and each cell one row
        /// tall, so the rows line up across columns; scrolled sideways when wider than the view.</summary>
        private void Table(ShownBlock.Table table)
        {
            var theme = ui.Theme;
            using (ui.Scroll(NextId(), ScrollAxes.Horizontal, new Box { Gap = 0f, AlignY = Align.Start }))
            using (ui.Row(new Box { Gap = 0f, BorderColor = theme.Border, BorderWidth = theme.BorderWidth }))
            {
                for (var column = 0; column < table.Columns.Count; column++)
                {
                    var align = AlignOf(table.Columns[column]);
                    using (ui.Column(new Box { Gap = 0f }))
                    {
                        Cell(CellOf(table.Header, column), align, theme.Surface);
                        foreach (var row in table.Rows)
                            Cell(CellOf(row, column), align, 0u);
                    }
                }
            }
        }

        private static IReadOnlyList<RichTextRun> CellOf(IReadOnlyList<IReadOnlyList<RichTextRun>> cells, int column) =>
            column < cells.Count ? cells[column] : [];

        private void Cell(IReadOnlyList<RichTextRun> runs, Align align, uint background)
        {
            var theme = ui.Theme;
            using (ui.Row(new Box
            {
                Width = Sizing.Grow(),
                Height = theme.RowHeight,
                Padding = new Spacing(theme.FieldPaddingX, 0f),
                AlignX = align,
                AlignY = Align.Center,
                Background = background,
                Clip = true,
            }))
            {
                if (runs.Count > 0)
                    Text(runs, wrap: false);
            }
        }

        private static Align AlignOf(ColumnAlignment alignment) => alignment switch
        {
            ColumnAlignment.None or ColumnAlignment.Left => Align.Start,
            ColumnAlignment.Center => Align.Center,
            ColumnAlignment.Right => Align.End,
            _ => throw new InvalidOperationException($"No alignment {alignment}."),
        };
    }
}
