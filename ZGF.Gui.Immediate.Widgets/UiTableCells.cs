namespace ZGF.Gui.Immediate.Widgets;

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

    /// <summary>What copying the current cell takes: the part of <paramref name="shown"/> selected,
    /// else <paramref name="whole"/>, which may hold more than is shown.</summary>
    public string Copied(string shown, string whole) =>
        Text.HasSelection && (Text.Start > 0 || Text.End < shown.Length) ? Text.Of(shown) : whole;
}

public static partial class UiWidgets
{
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
