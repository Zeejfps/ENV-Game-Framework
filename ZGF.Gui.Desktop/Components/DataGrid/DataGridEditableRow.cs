using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// The floating editor that covers the focused row. It renders exactly like a <see cref="DataGridPreviewRow{TItem}"/>
/// — same background, selection bar, and preview cell per column — so the row reads unchanged, EXCEPT the one cell
/// currently being edited: that column's preview is swapped for its editor widget (built from
/// <see cref="DataGridColumn{TItem}.CreateEditor"/>), framed by a focus highlight. Only the active cell looks like
/// it's being edited; the rest keep their display rendering (tabular figures, custom cells, etc.).
///
/// The editors for every editable column are created up front and kept hidden until focused, so moving across
/// columns within a row just toggles which editor shows — the typed values persist on each editor until the row
/// commits. Only ever a couple of these exist (the focused row + the new-row strip), so the editor widgets are
/// never instantiated per data row.
/// </summary>
public sealed class DataGridEditableRow<TItem> : View
{
    private readonly DataGridColumns _geometry;
    private readonly DataGridColumn<TItem>[] _columns;
    private readonly DataGridStyle _style;
    private readonly RectView _bg;
    private readonly RectView _bar;
    private readonly RectView _focusCell;
    private readonly View[] _preview;
    private readonly View?[] _editors;
    private readonly bool[] _editable;
    private int _focusedCol = -1;

    public DataGridEditableRow(
        DataGridColumns geometry, DataGridColumn<TItem>[] columns, DataGridStyle style,
        ICanvas canvas, InputSystem input, DataGridEditSession session)
    {
        _geometry = geometry;
        _columns = columns;
        _style = style;

        _bg = new RectView { BackgroundColor = style.SelectedRow };
        _bar = new RectView { BackgroundColor = style.SelectionBar, IsVisible = false };
        AddChildToSelf(_bg);
        AddChildToSelf(_bar);

        // Every column shows its normal preview cell; the focused column hides it and shows the editor instead.
        _preview = new View[columns.Length];
        for (var i = 0; i < columns.Length; i++)
        {
            _preview[i] = DataGridCell.BuildPreview(columns[i], style, canvas);
            AddChildToSelf(_preview[i]);
        }

        // Frames only the cell currently being edited (the editor itself is transparent), so a single cell reads
        // as the active input. Drawn over the preview cells but under the editors.
        _focusCell = new RectView
        {
            BackgroundColor = style.Surface,
            BorderColor = BorderColorStyle.All(style.FocusRing),
            BorderSize = BorderSizeStyle.All(1),
            IsVisible = false,
        };
        AddChildToSelf(_focusCell);

        _editable = new bool[columns.Length];
        _editors = new View?[columns.Length];
        var ctx = new DataGridEditorContext(canvas, input, session, style);
        for (var i = 0; i < columns.Length; i++)
        {
            if (!columns[i].IsEditable) continue;
            _editable[i] = true;
            var editor = columns[i].CreateEditor!(ctx);
            editor.IsVisible = false;
            _editors[i] = editor;
            AddChildToSelf(editor);
        }
    }

    /// <summary>Loads the item into the row: every column renders its preview, and every editor takes its current
    /// value (so whichever cell is focused already holds the right text). <paramref name="isNewRow"/> picks the
    /// background to match the row underneath (the trailing new-row strip vs. a selected data row).</summary>
    public void Bind(in TItem item, bool isNewRow = false)
    {
        _bg.BackgroundColor = isNewRow ? _style.NewRow : _style.SelectedRow;
        _bar.IsVisible = _style.ShowSelectionBar && !isNewRow;

        for (var i = 0; i < _columns.Length; i++)
        {
            DataGridCell.BindPreview(_preview[i], _columns[i], item);
            if (_editable[i]) _columns[i].BindEditor?.Invoke(_editors[i]!, item);
        }
        ClearValidation(); // a freshly (re)bound row isn't invalid until a commit is attempted
        ApplyCellVisibility();
        SetDirty();
    }

    /// <summary>Writes every editable cell's current value back into the item/store via
    /// <see cref="DataGridColumn{TItem}.CommitEditor"/>. All editors are flushed (not just the focused one), so a
    /// row edited across several columns commits every change at once.</summary>
    public void Commit(in TItem item)
    {
        for (var i = 0; i < _columns.Length; i++)
            if (_editable[i]) _columns[i].CommitEditor?.Invoke(_editors[i]!, item);
    }

    /// <summary>Validates every editable cell (each column's <see cref="DataGridColumn{TItem}.ValidateEditor"/>,
    /// where present) AND renders the per-cell validity cue via <see cref="DataGridColumn{TItem}.MarkInvalid"/>.
    /// Returns true when all valid. The grid calls this before a non-forced commit so an invalid row stays open
    /// (visibly flagged) instead of writing a half-parsed value.</summary>
    public bool ShowValidation()
    {
        var allValid = true;
        for (var i = 0; i < _columns.Length; i++)
        {
            if (!_editable[i] || _columns[i].ValidateEditor is not { } validate) continue;
            var ok = validate(_editors[i]!);
            _columns[i].MarkInvalid?.Invoke(_editors[i]!, !ok);
            if (!ok) allValid = false;
        }
        return allValid;
    }

    /// <summary>Clears any validity cue on every editable cell (marks all valid).</summary>
    public void ClearValidation()
    {
        for (var i = 0; i < _columns.Length; i++)
            if (_editable[i]) _columns[i].MarkInvalid?.Invoke(_editors[i]!, false);
    }

    /// <summary>The editor widget for a column, or null if that column is read-only.</summary>
    public View? Editor(int column) => _editors[column];

    /// <summary>Whether the column hosts an editor in this row.</summary>
    public bool IsEditable(int column) => _editable[column];

    public int ColumnCount => _columns.Length;

    /// <summary>Swaps a single editable column from its preview to its editor (or pass -1 to show all previews).
    /// Only this cell gets the editor + focus frame; every other column keeps its display rendering, so editing
    /// reads as one active cell rather than the whole row changing.</summary>
    public void SetFocusedColumn(int column)
    {
        var col = column >= 0 && column < _columns.Length && _editable[column] ? column : -1;
        if (_focusedCol == col) return;
        _focusedCol = col;
        ApplyCellVisibility();
        SetDirty();
    }

    private void ApplyCellVisibility()
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            var editing = i == _focusedCol && _editable[i];
            _preview[i].IsVisible = !editing;
            if (_editors[i] is { } editor) editor.IsVisible = editing;
        }
    }

    /// <summary>Forces the row to re-lay out its cells (e.g. after a column resize while editing), even though
    /// its own rect is unchanged.</summary>
    internal void Relayout() => SetDirty();

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        Place(_bg, pos);
        Place(_bar, new RectF(pos.Left, pos.Bottom, 3f, pos.Height));

        Span<RectF> cells = stackalloc RectF[_columns.Length];
        _geometry.Resolve(pos, cells);

        if (_focusedCol >= 0)
        {
            _focusCell.IsVisible = true;
            Place(_focusCell, cells[_focusedCol]);
        }
        else
        {
            _focusCell.IsVisible = false;
        }

        for (var i = 0; i < _columns.Length; i++)
        {
            Place(_preview[i], cells[i]);
            if (_editors[i] is { } editor) Place(editor, cells[i]);
        }
    }

    private static void Place(View v, RectF r)
    {
        v.LeftConstraint = r.Left;
        v.BottomConstraint = r.Bottom;
        v.WidthConstraint = r.Width;
        v.HeightConstraint = r.Height;
        v.LayoutSelf();
    }
}
