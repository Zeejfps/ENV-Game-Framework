using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>The current cell of a table: the row <see cref="Rows"/> selects, the
/// <see cref="Column"/> picked in it, and the text selected in that cell as a
/// <see cref="UiWidgets.SelectableText"/> shows it: all of it whenever another cell becomes current,
/// so a copy takes the whole cell.</summary>
public sealed class TableCellSelection
{
    private const int None = -1;

    private int _textRow = None;
    private int _textKey;
    private int _textColumn = None;

    public TableCellSelection(ListSelection rows) => Rows = rows;

    public TableCellSelection() : this(new ListSelection())
    {
    }

    /// <summary>How far the current cell's text is scrolled to follow its caret.</summary>
    public float Shift;

    public ListSelection Rows { get; }

    public int Row => Rows.Index;

    /// <summary>The current column, or -1 for a row selected without a cell.</summary>
    public int Column { get; private set; } = None;

    public TextSelection Text { get; } = new();

    public bool HasCell => Rows.HasSelection && Column >= 0;

    public bool IsAt(int row, int column) => Row == row && Column == column;

    /// <summary>Selects row <paramref name="row"/> of <paramref name="count"/> and makes its cell in
    /// <paramref name="column"/> current, scrolling the row into view unless told otherwise.</summary>
    public void Select(int row, int count, int column, bool reveal = true)
    {
        Rows.Select(row, count, reveal);
        SelectColumn(column);
    }

    /// <summary>Makes the selected row's cell in <paramref name="column"/> current.</summary>
    public void SelectColumn(int column) => Column = column;

    /// <summary>Selects all of the current cell's text again the next time it is shown.</summary>
    public void SelectAllText() => _textRow = None;

    public void Clear()
    {
        Rows.Clear();
        Column = None;
        SelectAllText();
    }

    /// <summary>What copying the current cell takes: the part of <paramref name="shown"/> selected,
    /// else <paramref name="whole"/>, which may hold more than is shown.</summary>
    public string Copied(string shown, string whole)
    {
        if (!IsPartlySelected(shown)) return whole;
        return Text.Of(shown);
    }

    private bool IsPartlySelected(string shown)
    {
        if (!IsTextOfCurrentCell()) return false;
        if (!Text.HasSelection) return false;
        return Text.Start > 0 || Text.End < shown.Length;
    }

    private bool IsTextOfCurrentCell() => _textRow == Row && _textColumn == Column;

    /// <summary>Before the current cell of the row with <paramref name="key"/> is drawn showing
    /// <paramref name="shown"/>: selects all of it unless the text selected is already its own.</summary>
    internal void Show(int key, string shown)
    {
        if (IsTextOfCurrentCell() && key == _textKey) return;
        _textRow = Row;
        _textKey = key;
        _textColumn = Column;
        Shift = 0f;
        Text.Select(shown.Length, 0);
    }

    /// <summary>Collapses the text selected in the current cell to its caret; returns false when
    /// none was.</summary>
    internal bool CollapseText()
    {
        if (!IsTextOfCurrentCell()) return false;
        if (!Text.HasSelection) return false;
        Text.Select(Text.Caret, Text.Caret);
        return true;
    }

    /// <summary>After the current cell's text has had <paramref name="cell"/>'s pointer: a click that
    /// selected nothing selects it all again, so a click always leaves the whole cell selected.</summary>
    internal void SettleClick(Interaction cell, string shown)
    {
        if (cell.Clicked && !Text.HasSelection)
            Text.Select(shown.Length, 0);
    }
}

/// <summary>What a <see cref="TableRow.TextCell"/> shows and how.</summary>
internal readonly record struct CellText(
    string Shown,
    uint? Color,
    float? Size,
    string? Family,
    TextOverflow Overflow,
    UiIcon? Link,
    string LinkTooltip,
    string? Suffix,
    bool Expanded);

public static partial class UiWidgets
{
    /// <summary>
    /// Records the cell of <paramref name="column"/> that <paramref name="cell"/> interacts with in
    /// the row with <paramref name="key"/>, showing its text on a first line as tall as the theme's
    /// rows: as a <see cref="SelectableText"/>, or with a link icon as a <see cref="SelectableLink"/>,
    /// followed by the suffix small and muted; or every line of it as a
    /// <see cref="SelectableTextArea"/> while expanded. The text reads and moves
    /// <paramref name="selection"/> while <paramref name="current"/>, when the cell is outlined as
    /// <see cref="TableCellOutline"/> and a click that selected nothing selects it all. Returns true
    /// on the frame the link is followed.
    /// </summary>
    internal static bool TableCellText(this Ui ui, TableColumns columns, int column, Interaction cell, int key, TableCellSelection selection,
        bool current, bool focused, in CellText text)
    {
        if (cell.Hovered && text.Link == null)
            ui.SetCursor(MouseCursor.Text);
        if (current)
            selection.Show(key, text.Shown);
        var followed = false;
        using (ui.TableCell(columns, column, cell.Id, Align.Start))
        {
            if (current)
                ui.TableCellOutline(focused);
            if (text.Expanded)
                CellLines(ui, cell, selection, current, text);
            else
                followed = CellFirstLine(ui, cell, selection, current, text);
        }
        if (current)
            selection.SettleClick(cell, text.Shown);
        return followed;
    }

    private static void CellLines(Ui ui, Interaction cell, TableCellSelection selection, bool current, in CellText text)
    {
        var color = CellLinesColor(ui.Theme, text.Color, text.Link);
        ui.SelectableTextArea(cell, text.Shown, selection.Text, ref selection.Shift, current, ui.Theme.RowHeight, color, text.Size, text.Family,
            text.Overflow);
    }

    private static uint CellLinesColor(UiTheme theme, uint? color, UiIcon? link)
    {
        if (color != null) return color.Value;
        if (link != null) return theme.AccentText;
        return theme.Text;
    }

    private static bool CellFirstLine(Ui ui, Interaction cell, TableCellSelection selection, bool current, in CellText text)
    {
        var theme = ui.Theme;
        var followed = false;
        using (ui.Row(new Box { Height = theme.RowHeight, Gap = theme.GapTight }))
        {
            if (text.Link != null)
                followed = ui.SelectableLink(cell, text.Shown, selection.Text, ref selection.Shift, current, text.Link.Value, text.LinkTooltip, text.Color,
                    text.Size, text.Family, text.Overflow);
            else
                ui.SelectableText(cell, text.Shown, selection.Text, ref selection.Shift, current, text.Color, text.Size, text.Family, text.Overflow);
            if (text.Suffix != null)
                ui.Text(text.Suffix, new Box { Width = Sizing.Fit() }, color: theme.TextMuted, size: theme.FontSizeSmall);
        }
        return followed;
    }

    /// <summary>Outlines the table cell being recorded as the current one, over its content, in the
    /// focus color while its table has focus.</summary>
    public static void TableCellOutline(this Ui ui, bool focused)
    {
        var theme = ui.Theme;
        using (ui.Float(0f, 0f, box: new Box
               {
                   Width = Sizing.Grow(),
                   Height = Sizing.Grow(),
                   BorderColor = focused ? theme.FocusRing : theme.BorderStrong,
                   BorderWidth = focused ? theme.FocusRingWidth : theme.BorderWidth,
               }, z: 1, clipped: true)) { }
    }
}
