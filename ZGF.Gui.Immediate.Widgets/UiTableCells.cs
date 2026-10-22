using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>What <see cref="TableCellSelection.ApplyKey"/> made of a stroke.</summary>
public enum TableCellKey
{
    None,

    /// <summary>Left or Right asked for another column of the current row.</summary>
    Column,

    /// <summary>Escape collapsed the text selected in the current cell to its caret.</summary>
    Collapsed,

    /// <summary>Ctrl+C asked to copy the current cell.</summary>
    Copy,
}

/// <summary>The current cell of a table, by row and column, and the text selected in it as a
/// <see cref="UiWidgets.SelectableText"/> shows it: all of it when a cell is first selected, so a
/// copy takes the whole cell.</summary>
public sealed class TableCellSelection
{
    /// <summary>How far the current cell's text is scrolled to follow its caret.</summary>
    public float Shift;

    public int Row { get; private set; } = -1;

    /// <summary>The current column, or -1 for a row selected without a cell.</summary>
    public int Column { get; private set; } = -1;

    public TextSelection Text { get; } = new();

    public bool HasCell => Row >= 0 && Column >= 0;

    public bool IsAt(int row, int column) => Row == row && Column == column;

    /// <summary>Makes the cell at <paramref name="row"/> and <paramref name="column"/> current with
    /// all of <paramref name="shown"/> selected; nothing changes when it already is.</summary>
    public void Select(int row, int column, string shown)
    {
        if (IsAt(row, column)) return;
        Row = row;
        Column = column;
        Shift = 0f;
        SelectAll(shown);
    }

    public void SelectAll(string shown) => Text.Select(shown.Length, 0);

    public void Clear()
    {
        Row = Column = -1;
        Shift = 0f;
        Text.Select(0, 0);
    }

    /// <summary>After the current cell's text has had <paramref name="cell"/>'s pointer: a click that
    /// selected nothing selects it all again, so a click always leaves the whole cell selected.</summary>
    public void SettleClick(Interaction cell, string shown)
    {
        if (cell.Clicked && !Text.HasSelection)
            SelectAll(shown);
    }

    /// <summary>What <paramref name="stroke"/> does to the current cell while its table has focus:
    /// Left and Right ask for the <paramref name="column"/> before or after it, kept within
    /// <paramref name="minColumn"/> and <paramref name="maxColumn"/>, for the caller to select with
    /// its text; Escape collapses the text selected in it; Ctrl+C asks to copy it. Nothing while no
    /// row is current, nor for other modifiers.</summary>
    public TableCellKey ApplyKey(KeyStroke stroke, int minColumn, int maxColumn, out int column)
    {
        column = Column;
        if (Row < 0) return TableCellKey.None;
        var modifiers = stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Super);
        switch (modifiers, stroke.Key)
        {
            case (KeyModifiers.Control, KeyboardKey.C):
                return TableCellKey.Copy;
            case (0, KeyboardKey.LeftArrow):
                column = Math.Clamp(Column - 1, minColumn, maxColumn);
                return TableCellKey.Column;
            case (0, KeyboardKey.RightArrow):
                column = Math.Clamp(Column + 1, minColumn, maxColumn);
                return TableCellKey.Column;
            case (0, KeyboardKey.Escape) when Text.HasSelection:
                Text.Select(Text.Caret, Text.Caret);
                return TableCellKey.Collapsed;
            default:
                return TableCellKey.None;
        }
    }

    /// <summary>What copying the current cell takes: the part of <paramref name="shown"/> selected,
    /// else <paramref name="whole"/>, which may hold more than is shown.</summary>
    public string Copied(string shown, string whole) =>
        Text.HasSelection && (Text.Start > 0 || Text.End < shown.Length) ? Text.Of(shown) : whole;
}

public static partial class UiWidgets
{
    /// <summary>Interacts with the cell <paramref name="id"/> of a row of the
    /// <see cref="ListArea"/> <paramref name="listId"/>: the pointer over it, or over the icon of a
    /// <see cref="SelectableLink"/> in it, sets <paramref name="rowHovered"/>, and a press or a
    /// right- or middle-click on it focuses the list.</summary>
    public static Interaction ListCell(this Ui ui, int listId, int id, ref bool rowHovered)
    {
        var cell = ui.Interact(id);
        rowHovered |= cell.Hovered || ui.IsLinkIconHovered(cell);
        var picked = cell.Pressed || cell.ClickedWith(PointerButton.Right) || cell.ClickedWith(PointerButton.Middle);
        if (picked)
            ui.Focus(listId, takesText: false);
        return cell;
    }

    /// <summary>
    /// Records the cell of <paramref name="column"/> that <paramref name="cell"/> interacts with,
    /// showing <paramref name="shown"/> on a first line as tall as the theme's rows: as a
    /// <see cref="SelectableText"/>, or with a <paramref name="link"/> icon as a
    /// <see cref="SelectableLink"/> that <paramref name="linkTooltip"/> names, followed by
    /// <paramref name="suffix"/> small and muted; or every line of it as a
    /// <see cref="SelectableTextArea"/> while <paramref name="expanded"/>. The text reads and moves
    /// <paramref name="selection"/> while <paramref name="current"/>, when the cell is outlined as
    /// <see cref="TableCellOutline"/> and a click that selected nothing selects it all. Returns true
    /// on the frame the link is followed.
    /// </summary>
    public static bool TableCellText(this Ui ui, TableColumns columns, int column, Interaction cell, string shown, TableCellSelection selection,
        bool current, bool focused, uint? color = null, float? size = null, string? family = null, TextOverflow overflow = TextOverflow.Ellipsis,
        UiIcon? link = null, string linkTooltip = "", string? suffix = null, bool expanded = false)
    {
        if (cell.Hovered && link == null)
            ui.SetCursor(MouseCursor.Text);
        var followed = false;
        using (ui.TableCell(columns, column, cell.Id, Align.Start))
        {
            if (current)
                ui.TableCellOutline(focused);
            if (expanded)
            {
                var linesColor = CellLinesColor(ui.Theme, color, link);
                ui.SelectableTextArea(cell, shown, selection.Text, ref selection.Shift, current, ui.Theme.RowHeight, linesColor, size, family, overflow);
            }
            else
            {
                followed = CellFirstLine(ui, cell, shown, selection, current, color, size, family, overflow, link, linkTooltip, suffix);
            }
        }
        if (current)
            selection.SettleClick(cell, shown);
        return followed;
    }

    private static uint CellLinesColor(UiTheme theme, uint? color, UiIcon? link)
    {
        if (color != null) return color.Value;
        if (link != null) return theme.AccentText;
        return theme.Text;
    }

    private static bool CellFirstLine(Ui ui, Interaction cell, string shown, TableCellSelection selection, bool current, uint? color, float? size,
        string? family, TextOverflow overflow, UiIcon? link, string linkTooltip, string? suffix)
    {
        var theme = ui.Theme;
        var followed = false;
        using (ui.Row(new Box { Height = theme.RowHeight, Gap = theme.GapTight }))
        {
            if (link != null)
                followed = ui.SelectableLink(cell, shown, selection.Text, ref selection.Shift, current, link.Value, linkTooltip, color, size, family, overflow);
            else
                ui.SelectableText(cell, shown, selection.Text, ref selection.Shift, current, color, size, family, overflow);
            if (suffix != null)
                ui.Text(suffix, new Box { Width = Sizing.Fit() }, color: theme.TextMuted, size: theme.FontSizeSmall);
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
