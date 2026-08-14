using ZGF.Geometry;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// The always-visible entry row for a <see cref="DataGridView{TItem}"/>: one
/// <see cref="DataGridEditableRow{TItem}"/> over a blank draft, laid out by <c>DataGrid&lt;TItem&gt;</c> as the
/// <c>South</c> band under the body rather than scrolling with them. Because it sits outside the virtualized
/// list it costs one widget regardless of row count, and adding an entry never means scrolling to the end.
///
/// It shares the grid's <see cref="DataGridColumns"/>, so its cells stay aligned with the header and the body,
/// and insets its right edge by <see cref="DataGridStyle.ScrollbarWidth"/> for the same reason the header does
/// (the body is squeezed left by the scrollbar gutter).
///
/// Lifecycle: a click focuses the clicked cell; Tab/Shift+Tab move across columns; Enter (or Tab off the last
/// column) validates and appends through <c>onAdd</c>, then re-focuses so rapid entry keeps going. Clicking away
/// flushes the typed text back through each column's <see cref="DataGridColumn{TItem}.CommitEditor"/> but does
/// <em>not</em> append — a half-typed entry survives losing focus instead of inserting a row.
/// </summary>
public sealed class DataGridNewRowView<TItem> : View
{
    /// <summary>Height of the hairline separating the strip from the scrolling body above it.</summary>
    public const float SeparatorHeight = 1f;

    private readonly DataGridColumns _geometry;
    private readonly DataGridColumn<TItem>[] _columns;
    private readonly DataGridStyle _style;
    private readonly InputSystem _input;
    private readonly Func<TItem> _newDraft;
    private readonly Action<TItem> _onAdd;

    private readonly DataGridEditableRow<TItem> _row;
    private readonly RectView _separator;

    private TItem _draft;
    private int _focusedCol = -1;
    private bool _movingFocus;

    public DataGridNewRowView(
        DataGridColumns geometry,
        DataGridColumn<TItem>[] columns,
        DataGridStyle style,
        ICanvas canvas,
        InputSystem input,
        Func<TItem> newDraft,
        Action<TItem> onAdd)
    {
        _geometry = geometry;
        _columns = columns;
        _style = style;
        _input = input;
        _newDraft = newDraft;
        _onAdd = onAdd;
        _draft = newDraft();
        Height = style.RowHeight + SeparatorHeight;

        var session = new DataGridEditSession
        {
            Commit = OnEditorBlur,
            Cancel = () => EndEdit(flush: true),
            MoveNext = () => MoveColumn(+1),
            MovePrev = () => MoveColumn(-1),
            MoveDown = () => TryAdd(_focusedCol),
            MoveUp = OnMoveUp,
        };

        _separator = new RectView { BackgroundColor = style.Border };
        _row = new DataGridEditableRow<TItem>(geometry, columns, style, canvas, input, session);
        AddChildToSelf(_separator);
        AddChildToSelf(_row);
        _row.Bind(_draft, isNewRow: true);

        this.UseController(input, new NewRowController(this));
        _geometry.Changed += OnColumnsResized;
    }

    /// <summary>Whether a cell of the strip currently holds the edit focus.</summary>
    public bool IsEditing => _focusedCol >= 0;

    /// <summary>The column being edited, or -1.</summary>
    public int FocusedColumn => _focusedCol;

    /// <summary>Raised after a draft is appended through <c>onAdd</c>, so the owner can refresh the body's count.</summary>
    public event Action? RowAdded;

    /// <summary>Raised (with the current column) when the user presses Up from the strip — the owner moves the
    /// edit focus back into the last body row.</summary>
    public event Action<int>? MoveUpRequested;

    /// <summary>The on-screen rect of one of the strip's cells, in GUI coordinates — for anchoring
    /// editor-adjacent overlays (an autocomplete list, a date picker) to the strip the same way a body row
    /// anchors them. False before layout or for an out-of-range column.</summary>
    public bool TryGetCellRect(int column, out RectF rect)
    {
        rect = default;
        if (column < 0 || column >= _columns.Length) return false;
        var band = _row.Position;
        if (band.Height <= 0f || band.Width <= 0f) return false;

        Span<RectF> cells = stackalloc RectF[_columns.Length];
        _geometry.Resolve(band, cells);
        rect = cells[column];
        return true;
    }

    /// <summary>Puts the edit focus on <paramref name="column"/> (or the nearest editable column after it).
    /// The owner calls this when editing runs off the end of the last body row, so Enter/Tab flow into the
    /// entry strip.</summary>
    public void FocusCell(int column)
    {
        var col = ResolveEditableColumn(column);
        if (col < 0) return;

        _movingFocus = true;
        if (_focusedCol >= 0 && _focusedCol != col) BlurEditor(_focusedCol);
        _focusedCol = col;
        _row.SetFocusedColumn(col);
        FocusEditor(col);
        _movingFocus = false;
        SetDirty();
    }

    /// <summary>Validates the draft and appends it through <c>onAdd</c>, keeping the focus on the strip for the
    /// next entry. Returns false (leaving the cells open and flagged) when a column fails its
    /// <see cref="DataGridColumn{TItem}.ValidateEditor"/>.</summary>
    public bool CommitEdit() => TryAdd(_focusedCol);

    /// <summary>Re-reads the draft (through the <c>newDraft</c> supplier) and rebinds the cells. For when the
    /// owner's pending-entry state changed underneath the strip.</summary>
    public void Refresh()
    {
        _draft = _newDraft();
        _row.Bind(_draft, isNewRow: true);
        SetDirty();
    }

    private bool TryAdd(int columnToRefocus)
    {
        if (_focusedCol < 0) return false;
        if (!_row.ShowValidation()) return false; // invalid draft — stay put, cells stay flagged

        _row.Commit(_draft);
        _onAdd(_draft);
        Refresh();
        RowAdded?.Invoke();

        // Rapid entry: land back on the same cell of the now-blank strip.
        _movingFocus = true;
        BlurEditor(_focusedCol);
        _focusedCol = -1;
        _movingFocus = false;
        FocusCell(columnToRefocus);
        return true;
    }

    private void OnMoveUp()
    {
        var col = _focusedCol;
        EndEdit(flush: true);
        MoveUpRequested?.Invoke(col);
    }

    private void OnEditorBlur()
    {
        if (_movingFocus) return;
        EndEdit(flush: true);
    }

    // Ends the edit session. `flush` writes the typed text back through each column's CommitEditor — which is
    // how the owner's draft state (not the row) holds a half-typed entry — WITHOUT appending a row. Losing
    // focus must never insert: on an always-visible strip that would fire constantly.
    private void EndEdit(bool flush)
    {
        if (_focusedCol < 0) return;
        if (flush) _row.Commit(_draft);

        _movingFocus = true;
        BlurEditor(_focusedCol);
        _movingFocus = false;
        _focusedCol = -1;
        _row.SetFocusedColumn(-1);
        _row.ClearValidation();
        if (flush) Refresh(); // re-read the draft so the previews show what was just flushed
        SetDirty();
    }

    private void MoveColumn(int dir)
    {
        if (_focusedCol < 0) return;
        var next = NextEditableColumn(_focusedCol, dir);
        if (next < 0)
        {
            // Off the end of the row: forward commits the entry; backward just stays put.
            if (dir > 0) TryAdd(FirstEditableColumn());
            return;
        }
        _movingFocus = true;
        BlurEditor(_focusedCol);
        _focusedCol = next;
        _row.SetFocusedColumn(next);
        FocusEditor(next);
        _movingFocus = false;
        SetDirty();
    }

    private void OnPressed(PointF point)
    {
        var band = _row.Position;
        if (band.Height <= 0f) return;
        var column = _geometry.HitTest(band, point.X);
        FocusCell(column >= 0 ? column : FirstEditableColumn());
    }

    private void FocusEditor(int column)
    {
        if (_row.Editor(column) is { } view && _input.GetController(view) is IGridCellEditor ed) ed.BeginEdit();
    }

    private void BlurEditor(int column)
    {
        if (column < 0) return;
        if (_row.Editor(column) is { } view && _input.GetController(view) is IGridCellEditor ed) ed.EndEdit();
    }

    private void OnColumnsResized()
    {
        _row.Relayout();
        SetDirty();
    }

    private int FirstEditableColumn()
    {
        for (var i = 0; i < _columns.Length; i++) if (_columns[i].IsEditable) return i;
        return -1;
    }

    private int ResolveEditableColumn(int column)
    {
        if (column >= 0 && column < _columns.Length && _columns[column].IsEditable) return column;
        var next = NextEditableColumn(column, +1);
        return next >= 0 ? next : FirstEditableColumn();
    }

    private int NextEditableColumn(int from, int dir)
    {
        for (var i = from + dir; i >= 0 && i < _columns.Length; i += dir)
            if (_columns[i].IsEditable) return i;
        return -1;
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        var rowHeight = Math.Max(0f, Math.Min(pos.Height - SeparatorHeight, _style.RowHeight));

        // Inset the right edge by the scrollbar gutter, as the header does, so the strip's cells line up with
        // the body cells the scrollbar pushes left.
        var width = Math.Max(0f, pos.Width - _style.ScrollbarWidth);

        Place(_row, new RectF(pos.Left, pos.Bottom, width, rowHeight));
        Place(_separator, new RectF(pos.Left, pos.Bottom + rowHeight, pos.Width, Math.Max(0f, pos.Height - rowHeight)));
    }

    private static void Place(View v, RectF r)
    {
        v.LeftConstraint = r.Left;
        v.BottomConstraint = r.Bottom;
        v.WidthConstraint = r.Width;
        v.HeightConstraint = r.Height;
        v.LayoutSelf();
    }

    // Clicks are handled on the bubble phase, so an editor already showing in a cell (or any interactive cell
    // widget) consumes its own press first; only a press nothing else wanted becomes a focus-this-cell.
    private sealed class NewRowController : KeyboardMouseController
    {
        private readonly DataGridNewRowView<TItem> _strip;

        public NewRowController(DataGridNewRowView<TItem> strip) => _strip = strip;

        public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
        {
            if (e.Phase != EventPhase.Bubbling || e.State != InputState.Pressed) return;
            if (e.Button != MouseButton.Left) return;
            _strip.OnPressed(e.Mouse.Point);
            e.Consume();
        }
    }
}
