using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>One column of a <see cref="TableColumns"/>: its share of the width the fixed columns
/// leave, and the least it shrinks to. A <see cref="Fixed"/> width neither grows nor resizes. A
/// column of <see cref="Controls"/> holds controls of its row: pressing it selects the row but
/// never makes its cell the current one.</summary>
public readonly record struct TableColumn(float Weight = 1f, float MinWidth = 48f, float? Fixed = null, bool Controls = false);

/// <summary>How a table's <see cref="TableFrame.Header"/> shows and changes the sort: the column the
/// rows are sorted by, or -1, and the icon drawn after its label. Passing one makes every header
/// clickable.</summary>
public readonly record struct TableSort(int Column, UiIcon? Icon = null);

/// <summary>Records what the header cell of <paramref name="column"/> shows in place of its label,
/// <paramref name="hovered"/> while the pointer is over the cell, handed <paramref name="state"/> so
/// it can be a static lambda that captures nothing and allocates nothing.</summary>
public delegate void TableHeading<TState>(Ui ui, int column, bool hovered, in TState state);

/// <summary>How far a table's rows are scrolled, owned by the app as <c>SplitPane</c>'s size is, so
/// several documents shown in turn by one table each keep their own place.</summary>
public sealed class TableScroll
{
    public float X;
    public float Y;
}

/// <summary>
/// The column widths of a table, shared by its header and the cells of its rows so they line up.
/// Owned by the app; dragging a border in the header moves width between the columns either side,
/// and <see cref="Weights"/> gives the result to save and <see cref="Restore"/> takes it back.
/// <see cref="Sized"/> columns instead keep their own widths, however wide together, for a table
/// that scrolls sideways.
/// </summary>
public sealed class TableColumns
{
    private const int NoColumn = -1;

    private readonly TableColumn[] _columns;
    private readonly float[] _weights;
    private readonly float[] _widths;
    private bool _isDragging;

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

    /// <summary>Counts the changes to <see cref="Weights"/>: once for each border drag that moved one,
    /// when the button comes up, never on the frames in between, and once for each
    /// <see cref="Restore"/> that changes them. Watch it to tell when the widths should be saved.</summary>
    public int Changes { get; private set; }

    /// <summary>Takes back <see cref="Weights"/> saved from a table with the same columns; anything
    /// else is ignored.</summary>
    public void Restore(IReadOnlyList<float>? weights)
    {
        if (weights == null || weights.Count != _weights.Length || weights.Any(static w => !(w > 0f) || float.IsInfinity(w))) return;
        var changed = false;
        for (var i = 0; i < _weights.Length; i++)
        {
            var weight = IsSized ? MathF.Max(_columns[i].MinWidth, weights[i]) : weights[i];
            if (weight != _weights[i])
                changed = true;
            _weights[i] = weight;
            if (IsSized)
                _widths[i] = _weights[i];
        }
        if (changed)
            Changes++;
    }

    internal bool IsSelectable(int index) => !_columns[index].Controls;

    /// <summary>The selectable column <paramref name="by"/> steps on from <paramref name="from"/>,
    /// skipping those that aren't; <paramref name="from"/> again past the last one either way, and
    /// the first one when <paramref name="from"/> is no column.</summary>
    internal int StepSelectable(int from, int by)
    {
        if (from < 0)
            return FindFirstSelectable();
        for (var i = from + by; i >= 0 && i < _columns.Length; i += by)
        {
            if (IsSelectable(i))
                return i;
        }
        return from;
    }

    private int FindFirstSelectable()
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            if (IsSelectable(i))
                return i;
        }
        return NoColumn;
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
        var before = _weights[left];
        Resize(left, leftStart, borderAt);
        if (_weights[left] != before)
            _isDragging = true;
    }

    /// <summary>Counts the border drag that moved a width as one change, once it ended.</summary>
    internal void FinishDragging()
    {
        if (!_isDragging)
            return;
        _isDragging = false;
        Changes++;
    }

    private void Resize(int left, float leftStart, float borderAt)
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
    private const int TableSalt = 0x5441424C;
    private const int TableHeaderSalt = 0x54424844;
    private const int TableBorderSalt = 0x54424C42;
    private const int TableHeadingSalt = 0x54424C48;

    internal static int TableBorderId(int headerId, int column) => HashCode.Combine(headerId, TableBorderSalt, column);

    internal static int TableHeaderId(int tableId) => HashCode.Combine(tableId, TableHeaderSalt);

    /// <summary>Counts a border drag as one change of the widths once the button is up.</summary>
    private static void FinishColumnDragOnRelease(Ui ui, TableColumns columns)
    {
        if (ui.Input.IsDown(PointerButton.Left))
            return;
        columns.FinishDragging();
    }

    public static TableFrame Table(this Ui ui, string id, TableColumns columns, ListSelection rows, TableScroll? scroll = null) =>
        Table(ui, ui.GetId(id), columns, rows, scroll);

    /// <summary>
    /// Opens the table <paramref name="id"/> laid out in <paramref name="columns"/>, whose rows are
    /// picked whole into <paramref name="rows"/>: record its <see cref="TableFrame.Header"/>, then
    /// its <see cref="TableFrame.Body"/>. The table is a Tab stop and takes keyboard focus, but no
    /// text, under the id of its body; <paramref name="scroll"/> keeps how far its rows are scrolled,
    /// else the table does.
    /// </summary>
    public static TableFrame Table(this Ui ui, int id, TableColumns columns, ListSelection rows, TableScroll? scroll = null) =>
        OpenTable(ui, id, columns, rows, null, scroll);

    public static TableFrame Table(this Ui ui, string id, TableColumns columns, TableCellSelection cells, TableScroll? scroll = null) =>
        Table(ui, ui.GetId(id), columns, cells, scroll);

    /// <summary>A <see cref="Table(Ui, int, TableColumns, ListSelection, TableScroll?)"/> of cells,
    /// one of which <paramref name="cells"/> makes current in the row it selects: each cell takes
    /// the pointer on its own, Left and Right step between the selectable columns, Escape collapses
    /// the text selected in the current cell and Ctrl+C asks to copy it.</summary>
    public static TableFrame Table(this Ui ui, int id, TableColumns columns, TableCellSelection cells, TableScroll? scroll = null) =>
        OpenTable(ui, id, columns, cells.Rows, cells, scroll);

    private static TableFrame OpenTable(Ui ui, int id, TableColumns columns, ListSelection rows, TableCellSelection? cells, TableScroll? scroll)
    {
        var state = ui.GetState<TableState>(HashCode.Combine(id, TableSalt));
        state.Begin(id, columns, rows, cells, scroll, ui.Theme.RowHeight);
        state.Focused = ui.ListArea(id);
        return new TableFrame(ui, state);
    }

    /// <summary>
    /// Lays the columns of <paramref name="table"/> out across the content of its body as last laid
    /// out — a scroll's content less its gutter — less the rows' padding each side, then records a
    /// header row with each column's label in <paramref name="labels"/>. A border between two
    /// resizable columns can be dragged. <see cref="TableColumns.Sized"/> columns keep their widths
    /// instead, and the header shows them scrolled as the rows are. With a <paramref name="sort"/>,
    /// returns the column whose header was clicked, else -1. A <paramref name="heading"/> records
    /// each cell's content instead of its label.
    /// </summary>
    internal static int TableHeader<TState>(Ui ui, TableState table, ReadOnlySpan<string> labels, TableSort? sort, TableHeading<TState>? heading,
        in TState state)
    {
        var theme = ui.Theme;
        var columns = table.Columns;
        var headerId = TableHeaderId(table.Id);
        var paddingX = theme.ShellPaddingX;
        var scrollX = 0f;
        if (columns.IsSized)
            scrollX = table.Scroll.X;
        if (ui.TryGetContentRect(table.Id, out var body) || ui.TryGetRect(table.Id, out body))
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
        FinishColumnDragOnRelease(ui, columns);

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
                        heading(ui, i, cell.Hovered, in state);
                    else if (i < labels.Length && labels[i].Length > 0)
                        ui.Text(labels[i], new Box { Width = Sizing.Fit() }, color: cell.Hovered ? theme.Text : theme.TextMuted, size: theme.FontSizeSmall,
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
                           id: TableBorderId(headerId, i), clipped: true))
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
