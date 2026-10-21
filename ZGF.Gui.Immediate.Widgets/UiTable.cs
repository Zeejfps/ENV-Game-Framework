using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>One column of a <see cref="TableColumns"/>: its share of the width the fixed columns
/// leave, and the least it shrinks to. A <see cref="Fixed"/> width neither grows nor resizes.</summary>
public readonly record struct TableColumn(float Weight = 1f, float MinWidth = 48f, float? Fixed = null);

/// <summary>
/// The column widths of a table, shared by its <see cref="UiWidgets.TableHeader"/> and the
/// <see cref="UiWidgets.TableCell"/>s of its rows so they line up. Owned by the app; dragging a
/// border in the header moves width between the columns either side, and <see cref="Weights"/>
/// gives the result to save and <see cref="Restore"/> takes it back.
/// </summary>
public sealed class TableColumns
{
    private readonly TableColumn[] _columns;
    private readonly float[] _weights;
    private readonly float[] _widths;
    private float _grab;

    public TableColumns(params TableColumn[] columns)
    {
        _columns = columns;
        _weights = columns.Select(static c => MathF.Max(0.01f, c.Weight)).ToArray();
        _widths = new float[columns.Length];
    }

    public int Count => _columns.Length;

    /// <summary>How wide column <paramref name="index"/> was laid out this frame.</summary>
    public float Width(int index) => _widths[index];

    /// <summary>Every column's share of the width left to the resizable ones, as dragged.</summary>
    public IReadOnlyList<float> Weights => _weights;

    /// <summary>Takes back <see cref="Weights"/> saved from a table with the same columns; anything
    /// else is ignored.</summary>
    public void Restore(IReadOnlyList<float>? weights)
    {
        if (weights == null || weights.Count != _weights.Length || weights.Any(static w => !(w > 0f) || float.IsInfinity(w))) return;
        for (var i = 0; i < _weights.Length; i++)
            _weights[i] = weights[i];
    }

    internal bool Resizable(int index) => _columns[index].Fixed == null;

    internal void Layout(float width)
    {
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

    internal void Grab(float offset) => _grab = offset;

    /// <summary>Moves the border after column <paramref name="left"/> to <paramref name="pointer"/>
    /// less the grab offset, giving the width to or taking it from column <paramref name="right"/>.</summary>
    internal void Drag(int left, int right, float leftStart, float pointer)
    {
        var pair = _widths[left] + _widths[right];
        var minLeft = _columns[left].MinWidth;
        var minRight = _columns[right].MinWidth;
        var leftWidth = Math.Clamp(pointer - _grab - leftStart, minLeft, MathF.Max(minLeft, pair - minRight));
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

    /// <summary>
    /// Lays <paramref name="columns"/> out across the content of the element with
    /// <paramref name="bodyId"/> as last laid out — a scroll's content less its gutter — less
    /// <paramref name="paddingX"/> each side, which the rows should pad by too, then records a header
    /// row with each column's label in <paramref name="headers"/>. A border between two resizable
    /// columns can be dragged.
    /// </summary>
    public static void TableHeader(this Ui ui, string id, TableColumns columns, int bodyId, ReadOnlySpan<string> headers, float paddingX = 0f)
    {
        var theme = ui.Theme;
        var headerId = ui.GetId(id);
        if (ui.TryGetContentRect(bodyId, out var body) || ui.TryGetRect(bodyId, out body))
            columns.Layout(body.Width - paddingX * 2);

        var hasRect = ui.TryGetRect(headerId, out var header);
        var start = header.X + paddingX;
        var pointer = ui.Input.Pointer.X;
        Span<bool> hot = stackalloc bool[columns.Count];
        for (var i = 0; i < columns.Count - 1; i++)
        {
            var right = i + 1;
            if (!columns.Resizable(i) || !columns.Resizable(right))
            {
                start += columns.Width(i);
                continue;
            }
            var border = ui.Interact(HashCode.Combine(headerId, TableBorderSalt, i));
            hot[i] = border.Hovered || border.Held;
            if (hot[i])
                ui.SetCursor(MouseCursor.ResizeHorizontal);
            if (border.Pressed)
                columns.Grab(pointer - (start + columns.Width(i)));
            else if (border.Held && hasRect)
                columns.Drag(i, right, start, pointer);
            start += columns.Width(i);
        }

        using (ui.Row(headerId, new Box
        {
            Width = Sizing.Grow(),
            Height = theme.RowHeight,
            Gap = 0f,
            Padding = new Spacing(paddingX, 0f),
            AlignY = Align.Center,
            Background = theme.Surface,
        }))
        {
            for (var i = 0; i < columns.Count; i++)
            {
                using (ui.TableCell(columns, i))
                {
                    if (i < headers.Length && headers[i].Length > 0)
                        ui.Text(headers[i], new Box { Width = Sizing.Fit() }, color: theme.TextMuted, size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
                    if (i < columns.Count - 1 && columns.Resizable(i) && columns.Resizable(i + 1))
                    {
                        var borderId = HashCode.Combine(headerId, TableBorderSalt, i);
                        var grip = theme.SplitterSize;
                        using (ui.Float(1f, 0f, -grip / 2, 0f, new Box { Width = grip, Height = Sizing.Grow(), AlignX = Align.Center }, id: borderId))
                            ui.Leaf(new Box { Width = theme.SeparatorWidth, Height = Sizing.Grow(), Background = hot[i] ? theme.Accent : theme.Separator });
                    }
                }
            }
        }
        ui.Separator();
    }

    /// <summary>A cell of the row being recorded, as wide as column <paramref name="index"/> of
    /// <paramref name="columns"/>: its content clipped, vertically centered and inset from the
    /// borders.</summary>
    public static Ui.Scope TableCell(this Ui ui, TableColumns columns, int index) =>
        ui.Row(new Box
        {
            Width = columns.Width(index),
            Height = Sizing.Grow(),
            Gap = ui.Theme.GapTight,
            Padding = new Spacing(ui.Theme.GapTight, 0f),
            AlignY = Align.Center,
            Clip = true,
        });
}
