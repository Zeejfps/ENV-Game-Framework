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
    public CodeViewPosition Position =>
        new(RowHeight > 0f ? (int)(ScrollY / RowHeight) : 0, ScrollX, default, new TextPosition(Selected, CurrentColumn));

    /// <summary>Starts over on <paramref name="grid"/>: unsorted, unfiltered but for the flags
    /// filters kept for its kind of rows, nothing selected, at the top.</summary>
    public void Show(MetadataGrid grid)
    {
        if (ReferenceEquals(grid, Grid)) return;
        Grid = grid;
        Columns = null;
        Filters = new MetadataColumnFilter?[grid.Columns.Count];
        SortColumn = PendingReveal = EditingColumn = FlagsColumn = None;
        Cell.Clear();
        Descending = BeginEdit = FlagsOpen = false;
        Order = null;
        View = [];
        ViewKey = 0;
        ScrollX = ScrollY = 0f;
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

    /// <summary>Makes <paramref name="row"/>'s cell in <paramref name="column"/> the current one
    /// with all its text selected.</summary>
    internal void SelectCell(int row, int column) => Cell.Select(row, column, ShownAt(row, column));

    /// <summary>The selected row, or <see cref="None"/>.</summary>
    internal int Selected => Cell.Row;

    /// <summary>The current cell's column, or <see cref="None"/>.</summary>
    internal int CurrentColumn => Cell.Column;

    /// <summary>Whether the current cell is a multi-line one opened in place.</summary>
    internal bool IsOpen => Open.ContainsKey((Selected, CurrentColumn));

    /// <summary>The current cell's text as shown: all of it while open, else its first line.</summary>
    internal string ShownText => ShownAt(Selected, CurrentColumn);

    internal void SelectAllText() => Cell.SelectAll(ShownText);

    internal string ShownAt(int row, int column) =>
        row < 0 || column < 0 ? "" : Open.ContainsKey((row, column)) ? Grid!.Text(row, column) : Grid!.Lines(row, column).FirstLine;

    internal int RowAt(int index) => View[index];

    internal int IndexOf(int row) => Array.IndexOf(View, row);
}
