using ZGF.Geometry;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// One recycled, display-only grid row: a background, an optional selection bar, and one preview cell widget
/// per column (a plain text cell by default, or whatever a column's <see cref="DataGridColumn{TItem}.CreateCell"/>
/// builds). Cells lay out through the shared <see cref="DataGridColumns"/>, so they never drift from the header
/// or hit-testing. Rebound as the list scrolls — <see cref="Bind"/> only swaps data and visual state.
/// </summary>
public sealed class DataGridPreviewRow<TItem> : View
{
    private readonly DataGridColumns _geometry;
    private readonly DataGridColumn<TItem>[] _columns;
    private readonly DataGridStyle _style;

    private readonly RectView _bg;
    private readonly RectView _bar;
    private readonly View[] _cells;

    public DataGridPreviewRow(
        DataGridColumns geometry, DataGridColumn<TItem>[] columns, DataGridStyle style, ICanvas canvas)
    {
        _geometry = geometry;
        _columns = columns;
        _style = style;

        _bg = new RectView { BackgroundColor = style.Surface };
        _bar = new RectView { BackgroundColor = style.SelectionBar, IsVisible = false };
        AddChildToSelf(_bg);
        AddChildToSelf(_bar);

        _cells = new View[columns.Length];
        for (var i = 0; i < columns.Length; i++)
        {
            var cell = DataGridCell.BuildPreview(columns[i], style, canvas);
            _cells[i] = cell;
            AddChildToSelf(cell);
        }
    }

    public void Bind(in TItem item, in DataGridRowState state)
    {
        _bg.BackgroundColor = state.Selected ? _style.SelectedRow
            : state.Flash ? _style.FlashRow
            : state.Hovered ? _style.RowHover
            : state.NewRow ? _style.NewRow
            : state.Stripe && _style.Striped ? _style.Stripe
            : _style.Surface;
        _bar.IsVisible = _style.ShowSelectionBar && state.Selected;

        for (var i = 0; i < _columns.Length; i++)
            DataGridCell.BindPreview(_cells[i], _columns[i], item);

        SetDirty();
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        Place(_bg, pos);
        Place(_bar, new RectF(pos.Left, pos.Bottom, 3f, pos.Height));

        // When the row is taller than one line (it's expanded), cells stay in the top strip and the band
        // below is left for a grid-level overlay; otherwise the strip is the whole row.
        var strip = pos.Height > _style.RowHeight
            ? new RectF(pos.Left, pos.Top - _style.RowHeight, pos.Width, _style.RowHeight)
            : pos;

        Span<RectF> cells = stackalloc RectF[_columns.Length];
        _geometry.Resolve(strip, cells);
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
