using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// One pooled editable grid row: the per-row "editable variant" of <see cref="DataGridPreviewRow{TItem}"/>.
/// Editable columns host their real editor widget (built from <see cref="DataGridColumn{TItem}.CreateEditor"/>);
/// read-only columns reuse the same preview cell a display row would. Cells lay out through the shared
/// <see cref="DataGridColumns"/>, so they sit exactly over the preview row this floats above. Only ever a
/// couple of these exist (the focused row + the new-row strip), so the expensive editor widgets are never
/// instantiated per data row.
/// </summary>
public sealed class DataGridEditableRow<TItem> : View
{
    private readonly DataGridColumns _geometry;
    private readonly DataGridColumn<TItem>[] _columns;
    private readonly RectView _bg;
    private readonly View[] _cells;
    private readonly bool[] _editable;

    public DataGridEditableRow(
        DataGridColumns geometry, DataGridColumn<TItem>[] columns, DataGridStyle style,
        ICanvas canvas, InputSystem input, DataGridEditSession session)
    {
        _geometry = geometry;
        _columns = columns;

        _bg = new RectView { BackgroundColor = style.SelectedRow };
        AddChildToSelf(_bg);

        _cells = new View[columns.Length];
        _editable = new bool[columns.Length];
        var ctx = new DataGridEditorContext(canvas, input, session, style);
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].IsEditable)
            {
                _cells[i] = columns[i].CreateEditor!(ctx);
                _editable[i] = true;
            }
            else
            {
                _cells[i] = DataGridCell.BuildPreview(columns[i], style, canvas);
            }
            AddChildToSelf(_cells[i]);
        }
    }

    /// <summary>Loads the item into every cell: editable cells take their current value via
    /// <see cref="DataGridColumn{TItem}.BindEditor"/>, read-only cells render their preview.</summary>
    public void Bind(in TItem item)
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            if (_editable[i]) _columns[i].BindEditor?.Invoke(_cells[i], item);
            else DataGridCell.BindPreview(_cells[i], _columns[i], item);
        }
        SetDirty();
    }

    /// <summary>Writes every editable cell's current value back into the item/store via
    /// <see cref="DataGridColumn{TItem}.CommitEditor"/>.</summary>
    public void Commit(in TItem item)
    {
        for (var i = 0; i < _columns.Length; i++)
            if (_editable[i]) _columns[i].CommitEditor?.Invoke(_cells[i], item);
    }

    /// <summary>True when every editable cell's value is acceptable (each column's
    /// <see cref="DataGridColumn{TItem}.ValidateEditor"/>, where present). The grid checks this before a
    /// non-forced commit so an invalid row stays open instead of writing a half-parsed value.</summary>
    public bool Validate()
    {
        for (var i = 0; i < _columns.Length; i++)
            if (_editable[i] && _columns[i].ValidateEditor is { } validate && !validate(_cells[i]))
                return false;
        return true;
    }

    /// <summary>The editor widget for a column, or null if that column is read-only.</summary>
    public View? Editor(int column) => _editable[column] ? _cells[column] : null;

    /// <summary>Whether the column hosts an editor in this row.</summary>
    public bool IsEditable(int column) => _editable[column];

    public int ColumnCount => _columns.Length;

    /// <summary>Forces the row to re-lay out its cells (e.g. after a column resize while editing), even though
    /// its own rect is unchanged.</summary>
    internal void Relayout() => SetDirty();

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        Place(_bg, pos);

        Span<RectF> cells = stackalloc RectF[_columns.Length];
        _geometry.Resolve(pos, cells);
        for (var i = 0; i < _cells.Length; i++)
            Place(_cells[i], cells[i]);
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
