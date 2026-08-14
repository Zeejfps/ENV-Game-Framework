using ZGF.Geometry;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// The column-header band for a <see cref="DataGridView{TItem}"/>: the per-column labels with draggable
/// splitters between resizable columns and a click-to-sort affordance on sortable columns. Painted
/// immediate-mode and resolving its cells through the same <see cref="DataGridColumns"/> as the body rows, so
/// dividers line up with the columns and a drag moves both. The right edge is inset by
/// <see cref="DataGridStyle.ScrollbarWidth"/> so the labels sit over the body cells the scrollbar pushes left.
/// </summary>
public sealed class DataGridHeaderView<TItem> : View
{
    private const float GrabHalfWidth = 5f;
    private const float ArrowWidth = 8f;
    private const float ArrowGap = 5f;

    private readonly DataGridColumns _geometry;
    private readonly DataGridColumn<TItem>[] _columns;
    private readonly string[] _labels;
    private readonly DataGridStyle _style;

    private int _hotBoundary = -1;
    private string? _sortKey;
    private bool _sortAscending;

    /// <summary>Raised with a column's <see cref="DataGridColumn{TItem}.Key"/> when its (sortable) header is
    /// clicked. The consumer re-queries its source; the grid does not sort data itself.</summary>
    public event Action<string>? SortRequested;

    /// <summary>Raised (on press, after a non-sortable header is clicked) with the hit column's
    /// <see cref="DataGridColumn{TItem}.Key"/> — a hook for column-header affordances like a filter popover.
    /// Fires only for columns a <see cref="SortRequested"/> didn't already consume.</summary>
    public event Action<string>? HeaderPressed;

    public DataGridHeaderView(
        DataGridColumns geometry, DataGridColumn<TItem>[] columns, DataGridStyle style, InputSystem input)
    {
        _geometry = geometry;
        _columns = columns;
        _style = style;
        Height = style.HeaderHeight;

        _labels = new string[columns.Length];
        for (var i = 0; i < columns.Length; i++)
        {
            var header = columns[i].Header;
            var text = header.IsSet ? header.Value : null;
            _labels[i] = string.IsNullOrEmpty(text) ? columns[i].Key : text;
        }

        _geometry.Changed += SetDirty;
        this.UseController(input, () => new DataGridHeaderController<TItem>(this, input));
    }

    /// <summary>Sets which column the data is currently sorted by, so its header shows a direction arrow.
    /// The grid never sorts data itself, so the owner pushes the sort state it applied to its source here;
    /// a null <paramref name="columnKey"/> (or one no column matches) clears the indicator.</summary>
    public void SetSort(string? columnKey, bool ascending)
    {
        if (_sortKey == columnKey && _sortAscending == ascending) return;
        _sortKey = columnKey;
        _sortAscending = ascending;
        SetDirty();
    }

    /// <summary>The band the columns resolve against — the header rect minus the scrollbar gutter.</summary>
    internal RectF Band
    {
        get
        {
            var p = Position;
            return new RectF(p.Left, p.Bottom, Math.Max(0f, p.Width - _style.ScrollbarWidth), p.Height);
        }
    }

    /// <summary>The grabbable divider near <paramref name="x"/> (only dividers to the right of a resizable
    /// column are grabbable), or -1.</summary>
    internal int BoundaryAt(float x)
    {
        var band = Band;
        for (var d = 0; d < _columns.Length - 1; d++)
        {
            if (_geometry.KindOf(d) != ColumnWidthKind.Resizable) continue;
            if (Math.Abs(x - _geometry.BoundaryX(d, band)) <= GrabHalfWidth) return d;
        }
        return -1;
    }

    internal void SetHotBoundary(int boundary)
    {
        if (_hotBoundary == boundary) return;
        _hotBoundary = boundary;
        SetDirty();
    }

    internal void DragBoundaryTo(int boundary, float x)
    {
        var band = Band;
        Span<RectF> cells = stackalloc RectF[_columns.Length];
        _geometry.Resolve(band, cells);
        var desired = x - _geometry.Gap * 0.5f - cells[boundary].Left;
        _geometry.SetWidth(boundary, desired, _geometry.MaxResizableWidth(boundary, band, _style.MinFlexWidth));
    }

    /// <summary>Click on a non-divider part of the header: sorts by the hit column if it is sortable. Returns
    /// whether it acted.</summary>
    internal bool TrySort(float x)
    {
        var idx = _geometry.HitTest(Band, x);
        if (idx < 0 || !_columns[idx].Sortable) return false;
        SortRequested?.Invoke(_columns[idx].Key);
        return true;
    }

    /// <summary>Press on a non-divider, non-sortable part of the header: raises <see cref="HeaderPressed"/> for
    /// the hit column (a filter/menu affordance hook). Returns whether it acted.</summary>
    internal bool TryHeaderPress(float x)
    {
        if (HeaderPressed is null) return false;
        var idx = _geometry.HitTest(Band, x);
        if (idx < 0) return false;
        HeaderPressed.Invoke(_columns[idx].Key);
        return true;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        var pos = Position;
        var z = GetDrawZIndex();

        c.DrawRect(new DrawRectInputs
        {
            Position = pos,
            Style = new RectStyle { BackgroundColor = _style.HeaderSurface },
            ZIndex = z,
        });
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(pos.Left, pos.Bottom, pos.Width, 1f),
            Style = new RectStyle { BackgroundColor = _style.Border },
            ZIndex = z + 1,
        });

        var band = Band;
        Span<RectF> cells = stackalloc RectF[_columns.Length];
        _geometry.Resolve(band, cells);

        for (var i = 0; i < _columns.Length; i++)
        {
            var cell = cells[i];
            if (cell.Width <= 0f) continue;

            var align = _columns[i].Align;
            var style = LabelStyle(align);
            var sorted = _sortKey is not null && _columns[i].Key == _sortKey;
            var textCell = sorted
                ? new RectF(cell.Left, cell.Bottom, Math.Max(0f, cell.Width - (ArrowWidth + ArrowGap)), cell.Height)
                : cell;

            c.PushClip(cell);
            c.DrawText(new DrawTextInputs
            {
                Position = textCell,
                Text = _labels[i],
                Style = style,
                ZIndex = z + 2,
            });
            if (sorted)
            {
                var textWidth = c.MeasureTextWidth(_labels[i], style);
                var textEnd = align switch
                {
                    TextAlignment.End => textCell.Right,
                    TextAlignment.Center => Math.Min(textCell.Right, textCell.Center.X + textWidth * 0.5f),
                    _ => Math.Min(textCell.Right, textCell.Left + textWidth),
                };
                var arrowLeft = Math.Min(textEnd + ArrowGap, cell.Right - ArrowWidth);
                DrawSortArrow(c, new RectF(arrowLeft, cell.Bottom, ArrowWidth, cell.Height), _sortAscending, z + 2);
            }
            c.PopClip();
        }

        for (var d = 0; d < _columns.Length - 1; d++)
        {
            if (_geometry.KindOf(d) != ColumnWidthKind.Resizable) continue;
            var x = _geometry.BoundaryX(d, band);
            c.DrawRect(new DrawRectInputs
            {
                Position = new RectF(x, pos.Bottom, 1f, pos.Height),
                Style = new RectStyle { BackgroundColor = _hotBoundary == d ? _style.SelectionBar : _style.Border },
                ZIndex = z + 2,
            });
        }
    }

    private void DrawSortArrow(ICanvas c, RectF box, bool ascending, int z)
    {
        const float halfWidth = 3.5f;
        const float halfHeight = 2.5f;
        var cx = box.Center.X;
        var cy = box.Center.Y;
        var apexY = ascending ? cy + halfHeight : cy - halfHeight;
        var baseY = ascending ? cy - halfHeight : cy + halfHeight;

        Line(c, new PointF(cx - halfWidth, baseY), new PointF(cx, apexY), z);
        Line(c, new PointF(cx, apexY), new PointF(cx + halfWidth, baseY), z);
    }

    private void Line(ICanvas c, PointF start, PointF end, int z) => c.DrawLine(new DrawLineInputs
    {
        Start = start,
        End = end,
        Thickness = 1.5f,
        Color = _style.SelectionBar,
        ZIndex = z,
        Cap = LineCap.Round,
    });

    private TextStyle LabelStyle(TextAlignment align) => new()
    {
        TextColor = _style.HeaderText,
        FontSize = 12.5f,
        FontWeight = FontWeight.Bold,
        HorizontalAlignment = align,
        VerticalAlignment = TextAlignment.Center,
    };
}

/// <summary>
/// Drives a <see cref="DataGridHeaderView{TItem}"/>: dragging a column splitter resizes that column, a plain
/// click on a sortable header sorts it. A press on a divider steals focus so the drag keeps tracking off the
/// header (the scrollbar-thumb pattern); hovering a divider lights it up.
/// </summary>
internal sealed class DataGridHeaderController<TItem> : KeyboardMouseController
{
    private readonly DataGridHeaderView<TItem> _view;
    private readonly InputSystem _input;
    private int _dragging = -1;

    public DataGridHeaderController(DataGridHeaderView<TItem> view, InputSystem input)
    {
        _view = view;
        _input = input;
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;

        if (_dragging >= 0)
        {
            _view.DragBoundaryTo(_dragging, e.Mouse.Point.X);
            e.Consume();
            return;
        }

        _view.SetHotBoundary(_view.BoundaryAt(e.Mouse.Point.X));
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;
        if (_dragging < 0) _view.SetHotBoundary(-1);
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling || e.Button != MouseButton.Left) return;

        if (_dragging < 0 && e.State == InputState.Pressed)
        {
            var boundary = _view.BoundaryAt(e.Mouse.Point.X);
            if (boundary >= 0)
            {
                _dragging = boundary;
                _view.SetHotBoundary(boundary);
                _input.StealFocus(this);
                e.Consume();
                return;
            }

            if (_view.TrySort(e.Mouse.Point.X) || _view.TryHeaderPress(e.Mouse.Point.X)) e.Consume();
            return;
        }

        if (_dragging >= 0 && e.State == InputState.Released)
        {
            _dragging = -1;
            _input.Blur(this);
            _view.SetHotBoundary(_view.BoundaryAt(e.Mouse.Point.X));
            e.Consume();
        }
    }

    public bool CanReleaseFocus() => _dragging < 0;
}
