using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Components.VerticalScrollBar;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
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

    /// <summary>Optional: builds a blank draft item for the trailing new-row strip. Supplying it together with
    /// <see cref="OnAddRow"/> turns on an always-editable append row at the end of the grid.</summary>
    public Func<TItem>? NewDraft { get; init; }

    /// <summary>Optional: appends the committed draft as a new row (the consumer adds it to its source). Used
    /// with <see cref="NewDraft"/>.</summary>
    public Action<TItem>? OnAddRow { get; init; }

    /// <summary>Hands the constructed body view back so the consumer can wire selection/activation and drive
    /// programmatic selection. Called once at build time.</summary>
    public Action<DataGridView<TItem>>? Ready { get; init; }

    /// <summary>Invoked with a column's <see cref="DataGridColumn{TItem}.Key"/> when its sortable header is clicked.</summary>
    public Action<string>? OnSort { get; init; }

    /// <summary>When true, the body runs in <see cref="DataGridView{TItem}.ExternalSelection"/> mode: it reports
    /// clicks/nav keys (wire them via <see cref="Ready"/>) and renders only what the owner pushes with
    /// <see cref="DataGridView{TItem}.SetSelectedKeys"/>, instead of owning selection itself.</summary>
    public bool ExternalSelection { get; init; }

    protected override IWidget Build(Context ctx)
    {
        var input = ctx.Require<InputSystem>();

        var columns = new DataGridColumn<TItem>[Columns.Count];
        for (var i = 0; i < Columns.Count; i++) columns[i] = Columns[i];

        var body = new DataGridView<TItem>(columns, Source, Style, ctx.Canvas, input, Key, NewDraft, OnAddRow)
        {
            ExternalSelection = ExternalSelection,
        };
        var header = new DataGridHeaderView<TItem>(body.Columns, columns, Style, input);
        if (OnSort != null) header.SortRequested += OnSort;
        Ready?.Invoke(body);

        var thumb = new VerticalScrollBarThumbView();

        return new BorderLayout
        {
            North = new Raw { View = header },
            Center = new KbmInput
            {
                Controller = _ => new VirtualListScrollController<DataGridPreviewRow<TItem>>(body.List, thumb),
                Child = new BorderLayout
                {
                    Center = new Raw { View = body },
                    East = new ScrollBar { Thumb = thumb },
                },
            },
        };
    }
}
