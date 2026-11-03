using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>Where a metadata grid is to go once it is laid out: to a row selected and scrolled into view, or
/// back to where it stood.</summary>
internal abstract record PendingGridScroll
{
    private PendingGridScroll()
    {
    }

    public sealed record Reveal(int Row) : PendingGridScroll;

    public sealed record Restore(GridPosition Position) : PendingGridScroll;
}

/// <summary>Everything a <see cref="MetadataGridWidget.MetadataGridView"/> keeps between frames of one
/// grid: the columns' widths, the sort and text filters, the selected cell and the text selected in it,
/// the scroll and the selected row's decoded details. Made for the grid a tab shows, and made anew when
/// the tab shows another, so that a grid starts unsorted, unfiltered but for the flags filters kept for
/// its kind of rows, nothing selected, at the top.</summary>
public sealed class MetadataGridState
{
    internal const int None = -1;

    internal readonly MetadataGrid Grid;
    internal TableColumns? Columns;
    internal readonly string[] Headers;
    internal readonly MetadataColumnFilter?[] Filters;
    internal int SortColumn = None;
    internal bool Descending;
    internal int[]? Order;
    internal int[] View = [];
    internal int ViewKey;
    internal readonly ListSelection Rows = new();
    internal readonly TableCellSelection Cell;
    internal readonly TableScroll Scroll = new();
    internal float RowHeight;
    internal PendingGridScroll? Pending;
    internal int EditingColumn = None;
    internal bool BeginEdit;
    internal int FlagsColumn = None;
    internal bool FlagsOpen;
    internal readonly Dictionary<(int Row, int Column), int> Open = new();
    internal readonly DetailTexts Details = new();
    internal readonly TallRowLayout TallRows = new();

    public MetadataGridState(MetadataGrid grid)
    {
        Grid = grid;
        Headers = ListHeaders(grid);
        Filters = new MetadataColumnFilter?[grid.Columns.Count];
        Cell = new TableCellSelection(Rows);
    }

    /// <summary>Where the view stands: the top row, the sideways scroll, and the selected row and current cell's
    /// column; where it is to be returned to while that waits for it to be laid out.</summary>
    public GridPosition Position
    {
        get
        {
            if (Pending is PendingGridScroll.Restore restoring)
                return restoring.Position;
            var topRow = RowHeight > 0f ? (int)(Scroll.Y / RowHeight) : 0;
            int? row = Selected == None ? null : Selected;
            int? column = CurrentColumn == None ? null : CurrentColumn;
            return new GridPosition(topRow, Scroll.X, row, column);
        }
    }

    private static string[] ListHeaders(MetadataGrid grid)
    {
        return grid.Columns
            .Select(static c => c.Name)
            .ToArray();
    }

    /// <summary>Returns the view to <paramref name="position"/> once it has been laid out.</summary>
    public void Restore(GridPosition position) => Pending = new PendingGridScroll.Restore(position);

    /// <summary>Selects <paramref name="row"/> and scrolls it into view once laid out.</summary>
    public void Reveal(int row) => Pending = new PendingGridScroll.Reveal(row);

    /// <summary>Sorts by <paramref name="column"/>, the other way when it already sorts by it, and
    /// scrolls the selected row back into view.</summary>
    internal void SortBy(int column)
    {
        Descending = SortColumn == column && !Descending;
        SortColumn = column;
        Order = Grid.Order(column, Descending);
        if (Selected != None)
            Pending = new PendingGridScroll.Reveal(Selected);
    }

    /// <summary>The filter of <paramref name="column"/>, made on first use; a column of flags gets
    /// the one <paramref name="flags"/> keeps for it.</summary>
    internal MetadataColumnFilter FilterOf(int column, MetadataFilters flags)
    {
        var filter = Filters[column];
        if (filter != null)
            return filter;
        filter = CreateFilter(column, flags);
        Filters[column] = filter;
        return filter;
    }

    private MetadataColumnFilter CreateFilter(int column, MetadataFilters flags)
    {
        if (!IsFlagsColumn(column))
            return new MetadataColumnFilter();
        return new MetadataColumnFilter { Flags = flags.For(Grid, column) };
    }

    private bool IsFlagsColumn(int column) => Grid.Columns[column].Flags != null;

    /// <summary>Filters and sorts the rows again when a filter or the sort changed.</summary>
    internal void UpdateView(MetadataFilters flags)
    {
        var key = ComputeViewKey(flags);
        if (key == ViewKey) return;
        var selected = Selected;
        ViewKey = key;
        View = Grid.Filter(Order, Filters);
        KeepSelected(selected);
    }

    /// <summary>Selects <paramref name="row"/> where the new view shows it; nothing when it doesn't.</summary>
    private void KeepSelected(int row)
    {
        var index = IndexOf(row);
        if (index < 0)
        {
            Rows.Clear();
            return;
        }
        Rows.Select(index, View.Length, reveal: false);
    }

    private int ComputeViewKey(MetadataFilters flags)
    {
        var key = new HashCode();
        key.Add(SortColumn);
        key.Add(Descending);
        for (var c = 0; c < Grid.Columns.Count; c++)
        {
            if (IsFlagsColumn(c))
                key.Add(FilterOf(c, flags).Flags!.Revision);
            key.Add(Filters[c]?.Text);
        }
        return key.ToHashCode() | 1;
    }

    internal void StartEditingFilter(int column)
    {
        EditingColumn = column;
        BeginEdit = true;
    }

    internal void StopEditingFilter()
    {
        EditingColumn = None;
    }

    internal void ToggleFlagsFlyout(int column)
    {
        FlagsOpen = !IsFlagsFlyoutOpenFor(column);
        FlagsColumn = column;
    }

    internal bool IsFlagsFlyoutOpenFor(int column) => FlagsOpen && FlagsColumn == column;

    /// <summary>Makes <paramref name="row"/>'s cell in <paramref name="column"/> the current one
    /// with all its text selected, where the view shows the row.</summary>
    internal void SelectCell(int row, int column)
    {
        var index = IndexOf(row);
        if (index < 0) return;
        Cell.Select(index, View.Length, column, reveal: false);
    }

    /// <summary>The selected row of the grid, or <see cref="None"/>.</summary>
    internal int Selected
    {
        get
        {
            var index = Rows.Index;
            if (index < 0 || index >= View.Length) return None;
            return View[index];
        }
    }

    /// <summary>The current cell's column, or <see cref="None"/>.</summary>
    internal int CurrentColumn => Cell.Column;

    internal bool HasCurrentCell => Selected >= 0 && CurrentColumn >= 0;

    /// <summary>Whether the current cell is a multi-line one opened in place.</summary>
    internal bool IsOpen => IsOpenAt(Selected, CurrentColumn);

    internal bool IsOpenAt(int row, int column) => Open.ContainsKey((row, column));

    /// <summary>Opens <paramref name="row"/>'s multi-line cell in <paramref name="column"/> in place:
    /// the cell grown downward to show every line, which leaves the rows above where they were.</summary>
    internal void Expand(int row, int column)
    {
        var lines = Grid.Text(row, column).Count(static ch => ch == '\n') + 1;
        Open[(row, column)] = lines;
    }

    internal void CollapseCurrent() => Open.Remove((Selected, CurrentColumn));

    /// <summary>The current cell's text as shown: all of it while open, else its first line.</summary>
    internal string ShownText => ShownAt(Selected, CurrentColumn);

    internal void SelectAllText() => Cell.SelectAllText();

    internal string ShownAt(int row, int column)
    {
        if (row < 0) return "";
        if (column < 0) return "";
        if (IsOpenAt(row, column)) return Grid.Text(row, column);
        return Grid.Lines(row, column).FirstLine;
    }

    /// <summary>What copying takes: the current cell's text, or the whole row's without one.</summary>
    internal string CopiedText()
    {
        if (CurrentColumn < 0) return RowText(Selected);
        return CellText();
    }

    /// <summary>What copying the current cell takes: the part of its text selected, else all of it,
    /// lines hidden while it is closed included.</summary>
    internal string CellText() => Cell.Copied(ShownText, Grid.Text(Selected, CurrentColumn));

    internal string RowText(int row)
    {
        var grid = Grid;
        var cells = Enumerable.Range(0, grid.Columns.Count)
            .Select(c => grid.Text(row, c));
        return string.Join('\t', cells);
    }

    internal int RowAt(int index) => View[index];

    internal int IndexOf(int row) => Array.IndexOf(View, row);
}
