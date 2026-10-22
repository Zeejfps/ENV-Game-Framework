using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>Everything a <see cref="MetadataGridWidget.MetadataGridView"/> keeps between frames:
/// the columns' widths, the sort and text filters, the selected cell and the text selected in it,
/// the scroll and the selected row's decoded details. One per open tab, owned by the app.</summary>
public sealed class MetadataGridState
{
    internal const int None = -1;

    internal MetadataGrid? Grid;
    internal TableColumns? Columns;
    internal MetadataColumnFilter?[] Filters = [];
    internal int SortColumn = None;
    internal bool Descending;
    internal int[]? Order;
    internal int[] View = [];
    internal int ViewKey;
    internal readonly TableCellSelection Cell = new();
    internal float ScrollX;
    internal float ScrollY;
    internal float RowHeight;
    internal int PendingReveal = None;
    internal CodeViewPosition? PendingRestore;
    internal int EditingColumn = None;
    internal bool BeginEdit;
    internal int FlagsColumn = None;
    internal bool FlagsOpen;
    internal readonly Dictionary<(int Row, int Column), int> Open = new();
    internal readonly DetailTexts Details = new();

    /// <summary>Where the view stands, as a code view's position: the top row as the top line, the
    /// selected row and cell as the caret.</summary>
    public CodeViewPosition Position
    {
        get
        {
            var topLine = RowHeight > 0f ? (int)(ScrollY / RowHeight) : 0;
            var caret = new TextPosition(Selected, CurrentColumn);
            return new CodeViewPosition(topLine, ScrollX, default, caret);
        }
    }

    /// <summary>Starts over on <paramref name="grid"/>: unsorted, unfiltered but for the flags
    /// filters kept for its kind of rows, nothing selected, at the top.</summary>
    public void Show(MetadataGrid grid)
    {
        if (ReferenceEquals(grid, Grid)) return;
        Grid = grid;
        Columns = null;
        Filters = new MetadataColumnFilter?[grid.Columns.Count];
        SortColumn = None;
        PendingReveal = None;
        EditingColumn = None;
        FlagsColumn = None;
        Cell.Clear();
        Descending = false;
        BeginEdit = false;
        FlagsOpen = false;
        Order = null;
        View = [];
        ViewKey = 0;
        ScrollX = 0f;
        ScrollY = 0f;
        PendingRestore = null;
        Open.Clear();
        Details.Clear();
    }

    /// <summary>Returns the view to <paramref name="position"/> once it has been laid out.</summary>
    public void Restore(CodeViewPosition position)
    {
        PendingRestore = position;
        PendingReveal = None;
    }

    /// <summary>Selects <paramref name="row"/> and scrolls it into view once laid out.</summary>
    public void Reveal(int row)
    {
        PendingReveal = row;
        PendingRestore = null;
    }

    /// <summary>Sorts by <paramref name="column"/>, the other way when it already sorts by it, and
    /// scrolls the selected row back into view.</summary>
    internal void SortBy(int column)
    {
        Descending = SortColumn == column && !Descending;
        SortColumn = column;
        Order = Grid!.Order(column, Descending);
        if (Selected != None)
            PendingReveal = Selected;
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
        return new MetadataColumnFilter { Flags = flags.For(Grid!, column) };
    }

    private bool IsFlagsColumn(int column) => Grid!.Columns[column].Flags != null;

    /// <summary>Filters and sorts the rows again when a filter or the sort changed.</summary>
    internal void UpdateView(MetadataFilters flags)
    {
        var key = ComputeViewKey(flags);
        if (key == ViewKey) return;
        ViewKey = key;
        View = Grid!.Filter(Order, Filters);
    }

    private int ComputeViewKey(MetadataFilters flags)
    {
        var key = new HashCode();
        key.Add(SortColumn);
        key.Add(Descending);
        for (var c = 0; c < Grid!.Columns.Count; c++)
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

    /// <summary>Makes <paramref name="row"/>'s cell in <paramref name="column"/> the current one
    /// with all its text selected.</summary>
    internal void SelectCell(int row, int column) => Cell.Select(row, column, ShownAt(row, column));

    /// <summary>The selected row, or <see cref="None"/>.</summary>
    internal int Selected => Cell.Row;

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
        var lines = Grid!.Text(row, column).Count(static ch => ch == '\n') + 1;
        Open[(row, column)] = lines;
    }

    internal void CollapseCurrent() => Open.Remove((Selected, CurrentColumn));

    /// <summary>The current cell's text as shown: all of it while open, else its first line.</summary>
    internal string ShownText => ShownAt(Selected, CurrentColumn);

    internal void SelectAllText() => Cell.SelectAll(ShownText);

    internal string ShownAt(int row, int column)
    {
        if (row < 0) return "";
        if (column < 0) return "";
        if (IsOpenAt(row, column)) return Grid!.Text(row, column);
        return Grid!.Lines(row, column).FirstLine;
    }

    /// <summary>What copying takes: the current cell's text, or the whole row's without one.</summary>
    internal string CopiedText()
    {
        if (CurrentColumn < 0) return RowText(Selected);
        return CellText();
    }

    /// <summary>What copying the current cell takes: the part of its text selected, else all of it,
    /// lines hidden while it is closed included.</summary>
    internal string CellText() => Cell.Copied(ShownText, Grid!.Text(Selected, CurrentColumn));

    internal string RowText(int row)
    {
        var grid = Grid!;
        var cells = Enumerable.Range(0, grid.Columns.Count)
            .Select(c => grid.Text(row, c));
        return string.Join('\t', cells);
    }

    internal int RowAt(int index) => View[index];

    internal int IndexOf(int row) => Array.IndexOf(View, row);
}
