using ZGF.Geometry;
using ZGF.Gui.Desktop.Components.VirtualWidgetList;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// The body of a data grid: a virtualized, recycled list of <see cref="DataGridPreviewRow{TItem}"/> over an
/// <see cref="IDataGridSource{TItem}"/>, with row selection and click/activate events. Cells lay out through a
/// shared <see cref="DataGridColumns"/> (also read by the header and the resize controller). Only the visible
/// window of rows is materialized, so a million-row source costs the same as a screenful.
///
/// This is the imperative core; <c>DataGrid&lt;TItem&gt;</c> (the declarative widget) wraps it with a header and
/// a synced scrollbar. Selection is by row index for now — stable-key selection is layered in the ledger refactor.
/// </summary>
public sealed class DataGridView<TItem> : View
{
    private readonly IDataGridSource<TItem> _source;
    private readonly DataGridColumn<TItem>[] _columns;
    private readonly ICanvas _canvas;
    private readonly InputSystem _input;
    private readonly VirtualWidgetListView<DataGridPreviewRow<TItem>> _list;
    private readonly DataGridEditSession _session;
    private readonly DataGridKeyboardController<TItem> _keyboard;
    private readonly Func<TItem, long>? _keyOf;
    private readonly Func<TItem>? _newDraft;
    private readonly Action<TItem>? _onAddRow;
    private readonly bool _hasNewRow;
    private TItem? _draft;
    private readonly HashSet<long> _selected = new();
    private int _anchor = -1;

    private DataGridEditableRow<TItem>? _editor;
    private int _focusRow = -1;
    private int _focusCol = -1;
    private bool _editing;
    private bool _movingFocus;

    private int _expandedIndex = -1;
    private View? _expansionOverlay;
    private float _expansionHeight;
    private int _activateColumn = -1;
    private int _flashIndex = -1;

    public DataGridView(
        IReadOnlyList<DataGridColumn<TItem>> columns,
        IDataGridSource<TItem> source,
        DataGridStyle style,
        ICanvas canvas,
        InputSystem input,
        Func<TItem, long>? key = null,
        Func<TItem>? newDraft = null,
        Action<TItem>? onAddRow = null)
    {
        _source = source;
        _canvas = canvas;
        _input = input;
        _keyOf = key;
        _newDraft = newDraft;
        _onAddRow = onAddRow;
        _hasNewRow = newDraft != null && onAddRow != null;
        if (_hasNewRow) _draft = newDraft!();
        Style = style;
        _columns = new DataGridColumn<TItem>[columns.Count];
        for (var i = 0; i < columns.Count; i++) _columns[i] = columns[i];
        Columns = new DataGridColumns(BuildWidths(_columns));

        _session = new DataGridEditSession
        {
            Commit = OnEditorBlur,
            Cancel = CancelEdit,
            MoveNext = () => MoveColumn(+1),
            MovePrev = () => MoveColumn(-1),
            MoveDown = () => MoveRow(+1),
            MoveUp = () => MoveRow(-1),
        };

        _list = new VirtualWidgetListView<DataGridPreviewRow<TItem>>
        {
            ItemCount = EffectiveCount,
            RowHeight = style.RowHeight,
            CreateRow = () => new DataGridPreviewRow<TItem>(Columns, _columns, style, canvas),
            BindRow = BindRowAt,
        };
        _list.RowClicked += OnRowClicked;
        _list.RowActivated += OnRowActivated;
        _list.RowContextRequested += (i, p) => RowContextRequested?.Invoke(i, p);
        _keyboard = new DataGridKeyboardController<TItem>(this, input);
        _list.UseController(input, _keyboard);
        _list.UseController(input, new VirtualWidgetListController<DataGridPreviewRow<TItem>>(_list));
        Columns.Changed += OnColumnsResized;

        AddChildToSelf(_list);

        void BindRowAt(DataGridPreviewRow<TItem> row, int index)
        {
            if (!IsNewRowIndex(index)) _source.EnsureWindow(index, index);
            if (!TryGetRowItem(index, out var item, out var isNew)) return;
            row.Bind(item, new DataGridRowState(
                Selected: !isNew && _selected.Contains(KeyOf(index, item)),
                Hovered: _list.HoveredIndex == index || _list.ContextHighlightIndex == index,
                Stripe: !isNew && (index & 1) == 1,
                Focused: _editing && index == _focusRow,
                NewRow: isNew,
                Flash: !isNew && index == _flashIndex));
        }
    }

    /// <summary>The number of rows the list shows: the source rows plus the trailing new-row strip when one
    /// is configured.</summary>
    private int EffectiveCount => _source.Count + (_hasNewRow ? 1 : 0);

    private bool IsNewRowIndex(int index) => _hasNewRow && index == _source.Count;

    private bool TryGetRowItem(int index, out TItem item, out bool isNew)
    {
        isNew = IsNewRowIndex(index);
        if (isNew) { item = _draft!; return true; }
        return _source.TryGetItem(index, out item!);
    }

    public DataGridStyle Style { get; }
    public DataGridColumns Columns { get; }

    /// <summary>When true, the grid never mutates its own selection from input (click / arrow keys / begin-edit):
    /// it raises <see cref="RowClicked"/> and <see cref="NavKeyPressed"/> and renders exactly whatever the owner
    /// pushes via <see cref="SetSelectedKeys"/>. For grids whose selection lives in an external model (e.g. a
    /// store that also drives totals, bulk ops, or navigation). Editing focus is unaffected.</summary>
    public bool ExternalSelection { get; init; }

    /// <summary>The selected rows as stable keys (the <c>key</c> selector's value, or the row index when no
    /// selector was given). Survives sorting/windowing when a key selector is used.</summary>
    public IReadOnlyCollection<long> SelectedKeys => _selected;

    /// <summary>The selected row indices. Meaningful only when no key selector is in use (then key == index);
    /// with a key selector, prefer <see cref="SelectedKeys"/>.</summary>
    public IReadOnlyCollection<int> SelectedIndices
    {
        get
        {
            var result = new int[_selected.Count];
            var i = 0;
            foreach (var k in _selected) result[i++] = (int)k;
            return result;
        }
    }

    public bool IsEditing => _editing;
    public int FocusedRow => _focusRow;
    public int FocusedColumn => _focusCol;

    public event Action? SelectionChanged;
    public event Action<int>? RowActivated;

    /// <summary>Right-click on a row: the hit row index (or -1) and the GUI-coordinate point. A consumer
    /// opens its context menu and may call <see cref="SetContextHighlight"/> to keep the row lit while open.</summary>
    public event Action<int, PointF>? RowContextRequested;

    /// <summary>Fires (with the row index) when a cell's edit is committed back through its column.</summary>
    public event Action<int>? CellCommitted;

    /// <summary>Fires (with the row index) when a row enters edit mode. An external-selection owner uses it to
    /// keep selection and editing mutually exclusive (the way a single-cell grid clears selection on focus).</summary>
    public event Action<int>? EditingStarted;

    /// <summary>Raised on a left click on a row (index, modifiers, GUI-coordinate point); index is -1 for a click
    /// in the empty area below the rows. Fires for every consumer; pair it with <see cref="ExternalSelection"/> to
    /// drive an external selection model.</summary>
    public event Action<int, InputModifiers, PointF>? RowClicked;

    /// <summary>Under <see cref="ExternalSelection"/>, raised when a navigation key (arrows / Enter / F2 / Escape)
    /// is pressed while not editing, so the owner can move its own cursor or begin an edit.</summary>
    public event Action<KeyboardKey>? NavKeyPressed;

    /// <summary>Highlights a row as if a context menu were open over it; pass null to clear.</summary>
    public void SetContextHighlight(int? index) => _list.SetContextHighlight(index);

    /// <summary>Sets (or clears, with null) the transient reveal-highlight on a row — e.g. to flash a
    /// just-inserted or navigated-to entry. Selection still takes visual precedence over the flash.</summary>
    public void SetFlash(int? index)
    {
        var next = index ?? -1;
        if (next == _flashIndex) return;
        _flashIndex = next;
        _list.NotifyItemsChanged();
    }

    internal VirtualWidgetListView<DataGridPreviewRow<TItem>> List => _list;

    /// <summary>Re-reads the source's count (after rows were added/removed) and rebinds. Clears any selection
    /// that fell out of range.</summary>
    public void Refresh()
    {
        _list.ItemCount = EffectiveCount;
        if (_keyOf == null) _selected.RemoveWhere(k => k >= _source.Count);
        _list.NotifyItemsChanged();
    }

    /// <summary>Scrolls just enough to bring the row at <paramref name="index"/> fully into view.</summary>
    public void EnsureRowVisible(int index) => _list.EnsureRowVisible(index);

    /// <summary>The on-screen rect of the cell currently being edited, in GUI coordinates — for positioning
    /// editor-adjacent overlays (autocomplete lists, date pickers). False when not editing or the focused row
    /// is scrolled out of range.</summary>
    public bool TryGetFocusedCellRect(out RectF rect)
    {
        rect = default;
        if (!_editing || _focusRow < 0 || _focusCol < 0) return false;
        if (!_list.TryGetRowRect(_focusRow, out var row)) return false;

        var stripHeight = Math.Min(row.Height, Style.RowHeight);
        var strip = new RectF(row.Left, row.Top - stripHeight, row.Width, stripHeight);
        Span<RectF> cells = stackalloc RectF[_columns.Length];
        Columns.Resolve(strip, cells);
        rect = cells[_focusCol];
        return true;
    }

    /// <summary>The on-screen rect of the row at <paramref name="index"/> (GUI coords); false when the row is
    /// outside the realized window. For positioning row-anchored overlays (a detail/split panel, a menu).</summary>
    public bool TryGetRowRect(int index, out RectF rect) => _list.TryGetRowRect(index, out rect);

    /// <summary>The on-screen rect of a single cell — column <paramref name="col"/> of the row at
    /// <paramref name="row"/> — resolved through the shared column geometry over the row's top one-line strip.
    /// False when the row is off-window or the column index is out of range. For cell-anchored overlays
    /// (autocomplete lists, a date picker) regardless of edit state.</summary>
    public bool TryGetCellRect(int row, int col, out RectF rect)
    {
        rect = default;
        if (col < 0 || col >= _columns.Length) return false;
        if (!_list.TryGetRowRect(row, out var r)) return false;
        var stripHeight = Math.Min(r.Height, Style.RowHeight);
        var strip = new RectF(r.Left, r.Top - stripHeight, r.Width, stripHeight);
        Span<RectF> cells = stackalloc RectF[_columns.Length];
        Columns.Resolve(strip, cells);
        rect = cells[col];
        return true;
    }

    /// <summary>The currently expanded row index, or -1.</summary>
    public int ExpandedIndex => _expandedIndex;

    /// <summary>The currently flashed row index, or -1.</summary>
    public int FlashIndex => _flashIndex;

    /// <summary>Expands the row at <paramref name="index"/> to make room for <paramref name="overlay"/> in a
    /// band of height <paramref name="overlayHeight"/> below its normal one-line strip (e.g. a detail panel).
    /// One row is expanded at a time; re-call as rows shift to keep the index current.</summary>
    public void SetExpanded(int index, View overlay, float overlayHeight)
    {
        if (!ReferenceEquals(_expansionOverlay, overlay))
        {
            if (_expansionOverlay != null) RemoveChildFromSelf(_expansionOverlay);
            _expansionOverlay = overlay;
            overlay.ZIndex = 4;
            AddChildToSelf(overlay);
        }
        _expandedIndex = index;
        _expansionHeight = overlayHeight;
        _list.RowHeightAt = RowHeightForIndex;
        _list.NotifyItemsChanged();
        SetDirty();
    }

    /// <summary>Collapses any expanded row and removes its overlay.</summary>
    public void Collapse()
    {
        if (_expandedIndex < 0) return;
        if (_expansionOverlay != null) { RemoveChildFromSelf(_expansionOverlay); _expansionOverlay = null; }
        _expandedIndex = -1;
        _expansionHeight = 0f;
        _list.RowHeightAt = null;
        _list.NotifyItemsChanged();
        SetDirty();
    }

    private float RowHeightForIndex(int index) =>
        index == _expandedIndex ? Style.RowHeight + _expansionHeight : Style.RowHeight;

    private void PositionExpansionOverlay()
    {
        if (_expansionOverlay == null) return;
        if (_expandedIndex >= 0 && _list.TryGetRowRect(_expandedIndex, out var rect) && rect.Height > Style.RowHeight)
        {
            _expansionOverlay.IsVisible = true;
            _expansionOverlay.LeftConstraint = rect.Left;
            _expansionOverlay.BottomConstraint = rect.Bottom;
            _expansionOverlay.WidthConstraint = rect.Width;
            _expansionOverlay.HeightConstraint = rect.Height - Style.RowHeight;
            _expansionOverlay.LayoutSelf();
        }
        else
        {
            _expansionOverlay.IsVisible = false;
        }
    }

    public bool IsSelected(int index) => _selected.Contains(KeyOf(index));

    /// <summary>Selects exactly <paramref name="index"/>, dropping any prior selection, and sets the range
    /// anchor there.</summary>
    public void SelectOnly(int index)
    {
        _selected.Clear();
        _selected.Add(KeyOf(index));
        _anchor = index;
        AfterSelectionChange();
    }

    /// <summary>Adds or removes <paramref name="index"/> from the selection (Ctrl/Cmd-click), moving the
    /// anchor there.</summary>
    public void ToggleSelected(int index)
    {
        var key = KeyOf(index);
        if (!_selected.Add(key)) _selected.Remove(key);
        _anchor = index;
        AfterSelectionChange();
    }

    /// <summary>Replaces the selection with the contiguous range from the current anchor to
    /// <paramref name="index"/> (Shift-click). With no anchor yet, selects just <paramref name="index"/>.</summary>
    public void SelectRangeTo(int index)
    {
        var from = _anchor < 0 ? index : _anchor;
        _selected.Clear();
        for (var i = Math.Min(from, index); i <= Math.Max(from, index); i++) _selected.Add(KeyOf(i));
        AfterSelectionChange();
    }

    private long KeyOf(int index)
    {
        if (_keyOf == null) return index;
        return _source.TryGetItem(index, out var item) ? _keyOf(item) : index;
    }

    private long KeyOf(int index, TItem item) => _keyOf?.Invoke(item) ?? index;

    public void ClearSelection()
    {
        if (_selected.Count == 0) return;
        _selected.Clear();
        _anchor = -1;
        AfterSelectionChange();
    }

    /// <summary>Replaces the rendered selection with exactly <paramref name="keys"/> (values from the <c>key</c>
    /// selector) and rebinds. For external selection owners — does not raise <see cref="SelectionChanged"/>, since
    /// the owner is the one telling the grid what's selected.</summary>
    public void SetSelectedKeys(IReadOnlyCollection<long> keys)
    {
        _selected.Clear();
        foreach (var k in keys) _selected.Add(k);
        _list.RefreshRows();
    }

    private void OnRowClicked(int index, InputModifiers mods, PointF point)
    {
        if (_editing) TryCommit(force: true); // clicking away — best-effort, like focus loss

        if (index >= 0) _activateColumn = ColumnAtPoint(index, point); // so a double-click edits the clicked cell
        RowClicked?.Invoke(index, mods, point);
        if (ExternalSelection) return;

        if (index < 0) { ClearSelection(); return; }

        if (mods.HasFlag(InputModifiers.Control) || mods.HasFlag(InputModifiers.Super)) ToggleSelected(index);
        else if (mods.HasFlag(InputModifiers.Shift)) SelectRangeTo(index);
        else SelectOnly(index);
    }

    private void OnRowActivated(int index)
    {
        RowActivated?.Invoke(index);
        if (HasEditableColumn) BeginEdit(index, _activateColumn >= 0 ? _activateColumn : FirstEditableColumn());
    }

    // The column under a point within row `index` (its top one-line strip), or -1 if the row isn't laid out.
    private int ColumnAtPoint(int index, PointF point)
    {
        if (!_list.TryGetRowRect(index, out var rect)) return -1;
        var stripHeight = Math.Min(rect.Height, Style.RowHeight);
        var strip = new RectF(rect.Left, rect.Top - stripHeight, rect.Width, stripHeight);
        return Columns.HitTest(strip, point.X);
    }

    private void AfterSelectionChange()
    {
        SelectionChanged?.Invoke();
        _list.RefreshRows();
    }

    // ---- Editing -------------------------------------------------------------------------------------------

    /// <summary>Begins editing the cell at (<paramref name="row"/>, <paramref name="column"/>). If the column
    /// is read-only, focus lands on the nearest editable column. Commits any edit already in progress.</summary>
    public void BeginEdit(int row, int column)
    {
        if (!HasEditableColumn || row < 0 || row >= EffectiveCount) return;
        if (_editing) TryCommit(force: true); // switching rows — best-effort commit the one we're leaving

        if (!IsNewRowIndex(row)) _source.EnsureWindow(row, row);
        if (!TryGetRowItem(row, out var item, out var isNew)) return;

        EnsureEditorRow();
        _focusRow = row;
        _focusCol = ResolveEditableColumn(column);
        _editing = true;
        if (!ExternalSelection) { if (isNew) ClearSelection(); else SelectOnly(row); }
        EditingStarted?.Invoke(row);
        _editor!.Bind(item);
        PositionEditor();
        FocusEditor(_focusCol);
        SetDirty();
    }

    /// <summary>Commits the editing row's values back through its columns and ends the edit session. Editing
    /// the trailing new-row strip instead appends a row through the configured add callback. Returns false
    /// (keeping the row open) when a cell fails its <see cref="DataGridColumn{TItem}.ValidateEditor"/>.</summary>
    public bool CommitEdit() => TryCommit(force: false);

    // force = best-effort (focus lost / clicking away): skip the validation gate and end regardless, writing
    // whatever each cell can parse. Non-forced (Enter / row move): an invalid row stays open and nothing writes.
    private bool TryCommit(bool force)
    {
        if (!_editing || _editor == null) return true;
        if (!force && !_editor.Validate()) return false;

        if (IsNewRowIndex(_focusRow))
        {
            AddNewRow();
            EndEdit();
            Refresh();
            return true;
        }
        if (_source.TryGetItem(_focusRow, out var item))
        {
            _editor.Commit(item);
            CellCommitted?.Invoke(_focusRow);
        }
        EndEdit();
        return true;
    }

    // Writes the editor values into the draft, hands it to the consumer to append, and resets the draft.
    private void AddNewRow()
    {
        _editor!.Commit(_draft!);
        _onAddRow!(_draft!);
        CellCommitted?.Invoke(_focusRow);
        _draft = _newDraft!();
    }

    /// <summary>Ends the edit session without writing the editor's values back.</summary>
    public void CancelEdit() => EndEdit();

    private void OnEditorBlur()
    {
        if (_movingFocus) return;
        TryCommit(force: true); // focus lost — best-effort; never trap focus on an invalid cell
    }

    private void EndEdit()
    {
        if (!_editing) return;
        EndEditSilently();
        _input.StealFocus(_keyboard);
        _list.RefreshRows();
        SetDirty();
    }

    // Tears down the edit session without grabbing focus back or repainting — for when another BeginEdit
    // follows immediately (rapid new-row entry).
    private void EndEditSilently()
    {
        _movingFocus = true;
        BlurEditor(_focusCol);
        _movingFocus = false;
        _editing = false;
        _focusRow = -1;
        _focusCol = -1;
        if (_editor != null) _editor.IsVisible = false;
    }

    /// <summary>Handles a navigation key when not editing: arrows move the selection, Enter/F2 begin editing
    /// the current row, Escape clears. Returns whether the key was handled. Driven by
    /// <see cref="DataGridKeyboardController{TItem}"/>.</summary>
    internal bool HandleNavKey(KeyboardKey key)
    {
        if (_editing) return false;

        if (ExternalSelection)
        {
            switch (key)
            {
                case KeyboardKey.DownArrow:
                case KeyboardKey.UpArrow:
                case KeyboardKey.Enter:
                case KeyboardKey.NumpadEnter:
                case KeyboardKey.F2:
                case KeyboardKey.Escape:
                    NavKeyPressed?.Invoke(key);
                    return true;
                default:
                    return false;
            }
        }

        switch (key)
        {
            case KeyboardKey.DownArrow: MoveCursor(+1); return true;
            case KeyboardKey.UpArrow: MoveCursor(-1); return true;
            case KeyboardKey.Enter:
            case KeyboardKey.NumpadEnter:
            case KeyboardKey.F2:
                if (HasEditableColumn && _anchor >= 0) BeginEdit(_anchor, FirstEditableColumn());
                return true;
            case KeyboardKey.Escape:
                ClearSelection();
                return true;
            default:
                return false;
        }
    }

    private void MoveCursor(int dir)
    {
        if (_source.Count == 0) return;
        var target = Math.Clamp((_anchor < 0 ? 0 : _anchor + dir), 0, _source.Count - 1);
        SelectOnly(target);
        _list.EnsureRowVisible(target);
    }

    private void MoveColumn(int dir)
    {
        if (!_editing) return;
        var next = NextEditableColumn(_focusCol, dir);
        if (next < 0)
        {
            if (dir > 0) MoveRow(+1, FirstEditableColumn());
            else MoveRow(-1, LastEditableColumn());
            return;
        }
        _movingFocus = true;
        BlurEditor(_focusCol);
        _focusCol = next;
        FocusEditor(_focusCol);
        _movingFocus = false;
        PositionEditor();
        SetDirty();
    }

    private void MoveRow(int dir, int? column = null)
    {
        if (!_editing || _editor == null) return;

        // Enter / Tab-off-the-end on the new-row strip: append, then keep editing the fresh strip below.
        if (IsNewRowIndex(_focusRow) && dir > 0)
        {
            var col = ResolveEditableColumn(column ?? _focusCol);
            if (!_editor.Validate()) return; // invalid draft — stay on the new-row strip
            AddNewRow();
            EndEditSilently();
            Refresh();
            BeginEdit(_source.Count, col);
            return;
        }

        var target = _focusRow + dir;
        if (target < 0 || target >= EffectiveCount) { CommitEdit(); return; }

        if (!IsNewRowIndex(_focusRow) && !_editor.Validate()) return; // invalid — don't leave the row

        if (!IsNewRowIndex(_focusRow) && _source.TryGetItem(_focusRow, out var current))
        {
            _editor.Commit(current);
            CellCommitted?.Invoke(_focusRow);
        }

        if (!IsNewRowIndex(target)) _source.EnsureWindow(target, target);
        if (!TryGetRowItem(target, out var next, out var targetIsNew)) { CommitEdit(); return; }

        _movingFocus = true;
        BlurEditor(_focusCol);
        _focusRow = target;
        if (column.HasValue) _focusCol = ResolveEditableColumn(column.Value);
        _list.EnsureRowVisible(target);
        if (!ExternalSelection) { if (targetIsNew) ClearSelection(); else SelectOnly(target); }
        _editor.Bind(next);
        PositionEditor();
        FocusEditor(_focusCol);
        _movingFocus = false;
        SetDirty();
    }

    private void EnsureEditorRow()
    {
        if (_editor != null) return;
        _editor = new DataGridEditableRow<TItem>(Columns, _columns, Style, _canvas, _input, _session)
        {
            ZIndex = 10,
            IsVisible = false,
        };
        AddChildToSelf(_editor);
    }

    private void PositionEditor()
    {
        if (_editor == null) return;
        if (_focusRow >= 0 && _list.TryGetRowRect(_focusRow, out var rect))
        {
            // Cover only the top one-line strip — matters when the focused row is expanded (taller).
            var height = Math.Min(rect.Height, Style.RowHeight);
            _editor.IsVisible = true;
            _editor.LeftConstraint = rect.Left;
            _editor.BottomConstraint = rect.Top - height;
            _editor.WidthConstraint = rect.Width;
            _editor.HeightConstraint = height;
            _editor.LayoutSelf();
        }
        else
        {
            _editor.IsVisible = false;
        }
    }

    private void FocusEditor(int column)
    {
        if (_editor?.Editor(column) is { } view && _input.GetController(view) is IGridCellEditor ed) ed.BeginEdit();
    }

    private void BlurEditor(int column)
    {
        if (_editor?.Editor(column) is { } view && _input.GetController(view) is IGridCellEditor ed) ed.EndEdit();
    }

    private void OnColumnsResized()
    {
        _list.RefreshRows();
        _editor?.Relayout();
        SetDirty();
    }

    private bool HasEditableColumn
    {
        get
        {
            foreach (var c in _columns) if (c.IsEditable) return true;
            return false;
        }
    }

    private int FirstEditableColumn()
    {
        for (var i = 0; i < _columns.Length; i++) if (_columns[i].IsEditable) return i;
        return -1;
    }

    private int LastEditableColumn()
    {
        for (var i = _columns.Length - 1; i >= 0; i--) if (_columns[i].IsEditable) return i;
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
        _list.LeftConstraint = pos.Left;
        _list.BottomConstraint = pos.Bottom;
        _list.WidthConstraint = pos.Width;
        _list.HeightConstraint = pos.Height;
        _list.LayoutSelf();

        if (_expandedIndex >= 0) PositionExpansionOverlay();
        if (_editing && _editor != null) PositionEditor();
    }

    public override bool ClipsContent => true;

    protected override void OnDrawChildren(ICanvas c)
    {
        c.PushClip(Position);
        base.OnDrawChildren(c);
        c.PopClip();
    }

    private static ColumnWidth[] BuildWidths(DataGridColumn<TItem>[] columns)
    {
        var widths = new ColumnWidth[columns.Length];
        for (var i = 0; i < columns.Length; i++) widths[i] = columns[i].Width;
        return widths;
    }
}
