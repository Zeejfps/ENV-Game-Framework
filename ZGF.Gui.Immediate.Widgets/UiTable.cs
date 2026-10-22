using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>One column of a <see cref="TableColumns"/>: its share of the width the fixed columns
/// leave, and the least it shrinks to. A <see cref="Fixed"/> width neither grows nor resizes.</summary>
public readonly record struct TableColumn(float Weight = 1f, float MinWidth = 48f, float? Fixed = null);

/// <summary>How a <see cref="UiWidgets.TableHeader"/> shows and changes the sort: the column the
/// rows are sorted by, or -1, and the icon drawn after its label. Passing one makes every header
/// clickable.</summary>
public readonly record struct TableSort(int Column, UiIcon? Icon = null);

/// <summary>Records what the header cell of <paramref name="column"/> shows in place of its label,
/// <paramref name="hovered"/> while the pointer is over the cell.</summary>
public delegate void TableHeading(Ui ui, int column, bool hovered);

/// <summary>
/// The column widths of a table, shared by its <see cref="UiWidgets.TableHeader"/> and the
/// <see cref="UiWidgets.TableCell"/>s of its rows so they line up. Owned by the app; dragging a
/// border in the header moves width between the columns either side, and <see cref="Weights"/>
/// gives the result to save and <see cref="Restore"/> takes it back. <see cref="Sized"/> columns
/// instead keep their own widths, however wide together, for a table that scrolls sideways.
/// </summary>
public sealed class TableColumns
{
    private readonly TableColumn[] _columns;
    private readonly float[] _weights;
    private readonly float[] _widths;

    public TableColumns(params TableColumn[] columns)
    {
        _columns = columns;
        _weights = columns.Select(static c => MathF.Max(0.01f, c.Weight)).ToArray();
        _widths = new float[columns.Length];
    }

    private TableColumns(IReadOnlyList<float> widths, float minWidth)
    {
        _columns = widths.Select(_ => new TableColumn(MinWidth: minWidth)).ToArray();
        _weights = widths.Select(w => MathF.Max(minWidth, w)).ToArray();
        _widths = _weights.ToArray();
        IsSized = true;
    }

    /// <summary>Columns <paramref name="widths"/> wide, each resized on its own by dragging its
    /// right border, down to <paramref name="minWidth"/>; <see cref="Weights"/> are their widths.</summary>
    public static TableColumns Sized(IReadOnlyList<float> widths, float minWidth = 32f) => new(widths, minWidth);

    public int Count => _columns.Length;

    /// <summary>Whether the columns keep their own widths rather than share the table's.</summary>
    public bool IsSized { get; }

    /// <summary>How wide column <paramref name="index"/> was laid out this frame.</summary>
    public float Width(int index) => _widths[index];

    /// <summary>How wide the columns are together.</summary>
    public float TotalWidth => _widths.Sum();

    /// <summary>Every column's share of the width left to the resizable ones, as dragged.</summary>
    public IReadOnlyList<float> Weights => _weights;

    /// <summary>Takes back <see cref="Weights"/> saved from a table with the same columns; anything
    /// else is ignored.</summary>
    public void Restore(IReadOnlyList<float>? weights)
    {
        if (weights == null || weights.Count != _weights.Length || weights.Any(static w => !(w > 0f) || float.IsInfinity(w))) return;
        for (var i = 0; i < _weights.Length; i++)
        {
            _weights[i] = IsSized ? MathF.Max(_columns[i].MinWidth, weights[i]) : weights[i];
            if (IsSized)
                _widths[i] = _weights[i];
        }
    }

    internal bool Resizable(int index) => _columns[index].Fixed == null;

    /// <summary>Whether the border after column <paramref name="index"/> can be dragged.</summary>
    internal bool HasBorder(int index) =>
        IsSized || (index < _columns.Length - 1 && Resizable(index) && Resizable(index + 1));

    internal void Layout(float width)
    {
        if (IsSized) return;
        var flexible = width;
        var total = 0f;
        for (var i = 0; i < _columns.Length; i++)
        {
            if (_columns[i].Fixed is { } fixedWidth)
                flexible -= fixedWidth;
            else
                total += _weights[i];
        }
        flexible = MathF.Max(0f, flexible);
        for (var i = 0; i < _columns.Length; i++)
            _widths[i] = _columns[i].Fixed ?? MathF.Max(_columns[i].MinWidth, MathF.Floor(flexible * _weights[i] / total));
    }

    /// <summary>Moves the border after column <paramref name="left"/> to <paramref name="borderAt"/>:
    /// a sized column alone takes the width; otherwise it is given to or taken from the column after it.</summary>
    internal void Drag(int left, float leftStart, float borderAt)
    {
        var minLeft = _columns[left].MinWidth;
        if (IsSized)
        {
            _widths[left] = _weights[left] = MathF.Max(minLeft, borderAt - leftStart);
            return;
        }
        var right = left + 1;
        var pair = _widths[left] + _widths[right];
        var minRight = _columns[right].MinWidth;
        var leftWidth = Math.Clamp(borderAt - leftStart, minLeft, MathF.Max(minLeft, pair - minRight));
        _widths[left] = leftWidth;
        _widths[right] = pair - leftWidth;
        for (var i = 0; i < _columns.Length; i++)
        {
            if (Resizable(i))
                _weights[i] = MathF.Max(1f, _widths[i]);
        }
    }
}

public static partial class UiWidgets
{
    private const int TableBorderSalt = 0x54424C42;
    private const int TableHeadingSalt = 0x54424C48;

    internal static int TableBorderId(int headerId, int column) => HashCode.Combine(headerId, TableBorderSalt, column);

    /// <summary>
    /// Lays <paramref name="columns"/> out across the content of the element with
    /// <paramref name="bodyId"/> as last laid out — a scroll's content less its gutter — less
    /// <paramref name="paddingX"/> each side, which the rows should pad by too, then records a header
    /// row with each column's label in <paramref name="headers"/>. A border between two resizable
    /// columns can be dragged. <see cref="TableColumns.Sized"/> columns keep their widths instead,
    /// and the header shows them scrolled by <paramref name="scrollX"/>, as the rows are. With a
    /// <paramref name="sort"/>, returns the column whose header was clicked, else -1. A
    /// <paramref name="heading"/> records each cell's content instead of its label.
    /// </summary>
    public static int TableHeader(this Ui ui, string id, TableColumns columns, int bodyId, ReadOnlySpan<string> headers, float paddingX = 0f,
        float scrollX = 0f, TableSort? sort = null, TableHeading? heading = null)
    {
        var theme = ui.Theme;
        var headerId = ui.GetId(id);
        if (ui.TryGetContentRect(bodyId, out var body) || ui.TryGetRect(bodyId, out body))
            columns.Layout(body.Width - paddingX * 2);

        var hasRect = ui.TryGetRect(headerId, out var header);
        var start = header.X + paddingX - scrollX;
        var pointer = ui.Input.Pointer.X;
        Span<bool> hot = stackalloc bool[columns.Count];
        for (var i = 0; i < columns.Count; i++)
        {
            if (!columns.HasBorder(i))
            {
                start += columns.Width(i);
                continue;
            }
            var border = ui.Interact(TableBorderId(headerId, i));
            hot[i] = border.Hovered || border.Held;
            if (hot[i])
                ui.SetCursor(MouseCursor.ResizeHorizontal);
            if (ui.TryGetDragStart(border, pointer - (start + columns.Width(i)), out var grab) && hasRect)
                columns.Drag(i, start, pointer - grab);
            start += columns.Width(i);
        }

        var clicked = -1;
        using (ui.Row(headerId, new Box
        {
            Width = Sizing.Grow(),
            Height = theme.RowHeight,
            Gap = 0f,
            Padding = new Spacing(paddingX, 0f),
            AlignY = Align.Center,
            Background = theme.TableHeaderBackground,
            Clip = columns.IsSized,
            ScrollX = scrollX,
        }))
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = sort is null ? default : ui.Clickable(HashCode.Combine(headerId, TableHeadingSalt, i));
                if (cell.Clicked)
                    clicked = i;
                using (sort is null ? ui.TableCell(columns, i) : ui.TableCell(columns, i, cell.Id))
                {
                    if (heading != null)
                        heading(ui, i, cell.Hovered);
                    else if (i < headers.Length && headers[i].Length > 0)
                        ui.Text(headers[i], new Box { Width = Sizing.Fit() }, color: cell.Hovered ? theme.Text : theme.TextMuted, size: theme.FontSizeSmall,
                            overflow: TextOverflow.Ellipsis);
                    if (sort is { Icon: { } icon } && sort.Value.Column == i)
                        ui.Icon(icon);
                }
            }

            var grip = theme.SplitterSize;
            var edge = paddingX - scrollX;
            for (var i = 0; i < columns.Count; i++)
            {
                edge += columns.Width(i);
                if (!columns.HasBorder(i)) continue;
                using (ui.Float(0f, 0f, edge - grip / 2, 0f, new Box { Width = grip, Height = theme.RowHeight, AlignX = Align.Center },
                           id: TableBorderId(headerId, i), clipped: columns.IsSized))
                    ui.Leaf(new Box { Width = theme.SeparatorWidth, Height = Sizing.Grow(), Background = hot[i] ? theme.Accent : theme.Separator });
            }
        }
        ui.Separator();
        return clicked;
    }

    /// <summary>A cell of the row being recorded, as wide as column <paramref name="index"/> of
    /// <paramref name="columns"/>: its content clipped, vertically centered and inset from the
    /// borders.</summary>
    public static Ui.Scope TableCell(this Ui ui, TableColumns columns, int index) => ui.Row(CellBox(ui, columns, index));

    /// <summary>A <see cref="TableCell(Ui, TableColumns, int)"/> recorded as element <paramref name="id"/>,
    /// for a cell that is hovered or clicked on its own, its content placed by <paramref name="alignY"/>
    /// in a row taller than it.</summary>
    public static Ui.Scope TableCell(this Ui ui, TableColumns columns, int index, int id, Align alignY = Align.Center) =>
        ui.Row(id, CellBox(ui, columns, index, alignY));

    private static Box CellBox(Ui ui, TableColumns columns, int index, Align alignY = Align.Center) =>
        new()
        {
            Width = columns.Width(index),
            Height = Sizing.Grow(),
            Gap = ui.Theme.GapTight,
            Padding = new Spacing(ui.Theme.GapTight, 0f),
            AlignY = alignY,
            Clip = true,
        };
}
