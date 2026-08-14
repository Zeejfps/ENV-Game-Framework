using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Components.VerticalScrollBar;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// A virtualized, column-driven data grid: a sortable/resizable header over a recycled body of preview rows,
/// with a synced scrollbar. The widgets-first counterpart to a hand-built grid — only the visible window of
/// rows is materialized, so a million-row <see cref="IDataGridSource{TItem}"/> costs the same as a screenful.
///
/// Define the shape with <see cref="DataGridColumn{TItem}"/>s (each a preview cell and, optionally, an editor
/// cell). Use <see cref="Ready"/> to capture the <see cref="DataGridView{TItem}"/> body for selection state
/// and row events, and <see cref="OnSort"/> to re-query the source when a sortable header is clicked.
/// </summary>
public sealed record DataGrid<TItem> : Widget
{
    public required IReadOnlyList<DataGridColumn<TItem>> Columns { get; init; }
    public required IDataGridSource<TItem> Source { get; init; }
    public DataGridStyle Style { get; init; } = DataGridStyle.Default;

    /// <summary>Optional stable-key selector. When given, selection is tracked by key so it survives sorting
    /// and windowing (the ledger keys on row id); otherwise selection is by row index.</summary>
    public Func<TItem, long>? Key { get; init; }

    /// <summary>Optional: supplies the pending draft item for the new-row strip. Supplying it together with
    /// <see cref="OnAddRow"/> pins an always-visible entry row below the body. Called whenever the strip
    /// rebinds, so a consumer whose draft lives in a view model returns a snapshot of that state.</summary>
    public Func<TItem>? NewDraft { get; init; }

    /// <summary>Optional: appends the committed draft as a new row (the consumer adds it to its source). Used
    /// with <see cref="NewDraft"/>.</summary>
    public Action<TItem>? OnAddRow { get; init; }

    /// <summary>Hands the constructed new-row strip back (only when <see cref="NewDraft"/>/<see cref="OnAddRow"/>
    /// are set), so a consumer can anchor editor-adjacent overlays to it the way it does for body rows.</summary>
    public Action<DataGridNewRowView<TItem>>? NewRowReady { get; init; }

    /// <summary>Hands the constructed body view back so the consumer can wire selection/activation and drive
    /// programmatic selection. Called once at build time.</summary>
    public Action<DataGridView<TItem>>? Ready { get; init; }

    /// <summary>Hands the constructed header band back so the consumer can push the sort state it applied to
    /// its source (<see cref="DataGridHeaderView{TItem}.SetSort"/>) and have the sorted column show an arrow.
    /// Called once at build time, before <see cref="Ready"/>.</summary>
    public Action<DataGridHeaderView<TItem>>? HeaderReady { get; init; }

    /// <summary>Invoked with a column's <see cref="DataGridColumn{TItem}.Key"/> when its sortable header is clicked.</summary>
    public Action<string>? OnSort { get; init; }

    /// <summary>Invoked (on press) with a non-sortable column's <see cref="DataGridColumn{TItem}.Key"/> when its
    /// header is clicked — a hook for column-header affordances such as a filter popover.</summary>
    public Action<string>? OnHeaderPress { get; init; }

    /// <summary>When true, the body runs in <see cref="DataGridView{TItem}.ExternalSelection"/> mode: it reports
    /// clicks/nav keys (wire them via <see cref="Ready"/>) and renders only what the owner pushes with
    /// <see cref="DataGridView{TItem}.SetSelectedKeys"/>, instead of owning selection itself.</summary>
    public bool ExternalSelection { get; init; }

    protected override IWidget Build(Context ctx)
    {
        var input = ctx.Require<InputSystem>();

        var columns = new DataGridColumn<TItem>[Columns.Count];
        for (var i = 0; i < Columns.Count; i++) columns[i] = Columns[i];

        var body = new DataGridView<TItem>(columns, Source, Style, ctx.Canvas, input, Key)
        {
            ExternalSelection = ExternalSelection,
        };
        var header = new DataGridHeaderView<TItem>(body.Columns, columns, Style, input);
        if (OnSort != null) header.SortRequested += OnSort;
        if (OnHeaderPress != null) header.HeaderPressed += OnHeaderPress;
        HeaderReady?.Invoke(header);
        Ready?.Invoke(body);

        // The entry row is its own widget in the South band, not a row of the list: it never scrolls away, and
        // the body stays a pure view of the source. The two are joined only by the edit focus flowing between
        // them (down off the last row lands here; Up from here goes back).
        DataGridNewRowView<TItem>? newRow = null;
        if (NewDraft is { } newDraft && OnAddRow is { } onAddRow)
        {
            newRow = new DataGridNewRowView<TItem>(body.Columns, columns, Style, ctx.Canvas, input, newDraft, onAddRow);
            newRow.RowAdded += body.Refresh;
            newRow.MoveUpRequested += col =>
            {
                if (Source.Count > 0) body.BeginEdit(Source.Count - 1, col);
            };
            body.EditMovedPastLastRow += newRow.FocusCell;
            NewRowReady?.Invoke(newRow);
        }

        var thumb = new VerticalScrollBarThumbView { MinHeight = Style.MinThumbHeight };

        return new BorderLayout
        {
            North = new Raw { View = header },
            South = newRow == null ? null : new Raw { View = newRow },
            Center = new KbmInput
            {
                Controller = _ => new VirtualListScrollController<DataGridPreviewRow<TItem>>(body.List, thumb),
                Child = new BorderLayout
                {
                    Center = new Raw { View = body },
                    East = new ScrollBar { Thumb = thumb, Style = ScrollBarStyleFrom(Style) },
                },
            },
        };
    }

    // The scrollbar inherits the grid's surface/border/muted-text colors so it themes with the rest of the grid
    // instead of falling back to the default grey.
    private static ScrollBarStyle ScrollBarStyleFrom(DataGridStyle s) => new()
    {
        TrackBackground = s.Surface,
        TrackBorderSize = new BorderSizeStyle { Left = 1 },
        TrackBorder = new BorderColorStyle { Left = s.Border },
        ThumbIdleBackground = s.Border,
        ThumbHoverBackground = s.HeaderText,
        ThumbBorderSize = BorderSizeStyle.All(0),
    };
}
