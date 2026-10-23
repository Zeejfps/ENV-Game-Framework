using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>What is decoded from a metadata grid's selected row, shown under it: a text in a code view,
/// or a table.</summary>
internal static class MetadataDetailsView
{
    private const int MaxTextLines = 12;
    private const float TextPaddingX = 24f;
    private const float MaxTableHeight = 250f;

    /// <summary>How tall <paramref name="row"/>'s details are in a grid <paramref name="width"/> wide;
    /// 0 when it has none.</summary>
    public static float Height(Ui<UiState> ui, MetadataGrid grid, DetailTexts texts, int row, float width)
    {
        if (row < 0) return 0f;
        if (!grid.HasDetails) return 0f;
        var theme = ui.Theme;
        var padding = theme.GapTight * 2;
        switch (grid.Details(row))
        {
            case MetadataTextDetails text:
                var size = texts.SizeOf(row, text.Text);
                return TextHeight(ui, size, width - Left(theme) - theme.ShellPaddingX) + padding;
            case MetadataTableDetails table:
                return MathF.Min(MaxTableHeight, (table.Rows.Count + 1) * theme.RowHeight + padding);
            default:
                return 0f;
        }
    }

    /// <summary>How tall a text is in a code view <paramref name="width"/> wide: a code line per
    /// line, up to <see cref="MaxTextLines"/> before it scrolls, plus the horizontal scrollbar's room
    /// when its longest line is wider.</summary>
    private static float TextHeight(Ui<UiState> ui, TextSize size, float width)
    {
        var theme = ui.Theme;
        var cell = ui.MeasureCell(theme.MonoFontSize, family: MonoFonts.Regular);
        var lineHeight = MathF.Max(theme.CodeLineHeight, MathF.Ceiling(cell.Height));
        var overflows = TextPaddingX + size.Longest * cell.Advance > width;
        var bar = overflows ? theme.ScrollBarWidth + theme.ScrollBarInset * 2 : 0f;
        return Math.Min(size.Lines, MaxTextLines) * lineHeight + bar;
    }

    private static float Left(UiTheme theme) => theme.ShellPaddingX + theme.IconSize;

    public static void Render(Ui<UiState> ui, MetadataGrid grid, DetailTexts texts, int row, float height)
    {
        var theme = ui.Theme;
        using var box = ui.Column(new Box
        {
            Width = Sizing.Grow(),
            Height = height,
            Gap = 0f,
            Padding = new Spacing(Left(theme), theme.GapTight, theme.ShellPaddingX, theme.GapTight),
            Background = theme.Surface,
        });
        using var scope = ui.Id(row);
        switch (grid.Details(row))
        {
            case MetadataTextDetails text:
                Text(ui, grid, texts, row, text);
                break;
            case MetadataTableDetails table:
                Table(ui, table);
                break;
        }
    }

    private static void Text(Ui<UiState> ui, MetadataGrid grid, DetailTexts texts, int row, MetadataTextDetails text)
    {
        var shown = texts.Get(row, grid.Title, text.Text);
        ui.CodeView("details", shown.Document, shown.View, gutter: false);
    }

    private static void Table(Ui<UiState> ui, MetadataTableDetails table)
    {
        var theme = ui.Theme;
        var share = 1f / table.Headers.Count;
        using (ui.Row(new Box { Height = theme.RowHeight, Gap = theme.GapTight, AlignY = Align.Center }))
        {
            foreach (var header in table.Headers)
                ui.Text(header, new Box { Width = Sizing.Percent(share) }, color: theme.TextMuted, size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
        }
        using (ui.Scroll("details", new Box { Height = Sizing.Grow(), Gap = 0f }))
        {
            foreach (var cells in table.Rows)
                TableRow(ui, cells, share);
        }
    }

    private static void TableRow(Ui<UiState> ui, IReadOnlyList<string> cells, float share)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Height = theme.RowHeight, Gap = theme.GapTight, AlignY = Align.Center }))
        {
            foreach (var cell in cells)
                ui.Text(cell, new Box { Width = Sizing.Percent(share) }, overflow: TextOverflow.Ellipsis);
        }
    }
}
