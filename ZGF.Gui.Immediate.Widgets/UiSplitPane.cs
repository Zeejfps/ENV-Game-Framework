using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>An open <see cref="UiWidgets.SplitPane"/>: record <see cref="First"/>, then
/// <see cref="Second"/>, each in its own scope, then dispose.</summary>
public readonly struct SplitPane : IDisposable
{
    private readonly Ui _ui;
    private readonly Ui.Scope _container;
    private readonly Interaction _divider;
    private readonly bool _horizontal;
    private readonly float _size;

    internal SplitPane(Ui ui, Ui.Scope container, Interaction divider, bool horizontal, float size)
    {
        _ui = ui;
        _container = container;
        _divider = divider;
        _horizontal = horizontal;
        _size = size;
    }

    /// <summary>The first pane — left or top — a clipped column at the split's size.</summary>
    public Ui.Scope First() => _ui.Column(_horizontal
        ? new Box { Width = _size, Height = Sizing.Grow(), Clip = true }
        : new Box { Width = Sizing.Grow(), Height = _size, Clip = true });

    /// <summary>Records the divider, then opens the second pane, a clipped column filling the rest.</summary>
    public Ui.Scope Second()
    {
        var style = _ui.Style;
        var thickness = WidgetTheme.For(_ui).SplitterSize;
        var color = _divider.Held ? style.Accent : _divider.Hovered ? style.TextMuted : style.Border;
        using (_ui.Box(_divider.Id, _horizontal
            ? new Box { Width = thickness, Height = Sizing.Grow() }
            : new Box { Width = Sizing.Grow(), Height = thickness }))
        {
            _ui.Paint((color, _horizontal), static (p, s) =>
            {
                var r = p.Rect;
                if (s._horizontal)
                    p.FillRect(new UiRect(r.X + MathF.Floor(r.Width / 2), r.Y, 1f, r.Height), s.color);
                else
                    p.FillRect(new UiRect(r.X, r.Y + MathF.Floor(r.Height / 2), r.Width, 1f), s.color);
            });
        }
        return _ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Clip = true });
    }

    public void Dispose() => _container.Dispose();
}

public static partial class UiWidgets
{
    private sealed class SplitState
    {
        public float Grab;
    }

    /// <summary>
    /// Two panes side by side (<see cref="Direction.Horizontal"/>) or stacked, split by a divider
    /// the user drags. <paramref name="size"/> is the first pane's width or height, owned by the
    /// app and written while dragging; it is shown kept between <paramref name="minFirst"/> and
    /// what leaves the second pane <paramref name="minSecond"/>. Fills the space it is given
    /// unless <paramref name="box"/> sizes it.
    /// </summary>
    public static SplitPane SplitPane(this Ui ui, string id, Direction direction, ref float size, float minFirst = 0f, float minSecond = 0f, Box box = default)
    {
        var horizontal = direction == Direction.Horizontal;
        var containerId = ui.GetId(id);
        var divider = ui.Interact(HashCode.Combine(containerId, "divider"));
        if (divider.Hovered || divider.Held)
            ui.SetCursor(horizontal ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);

        var hasContainer = ui.TryGetRect(containerId, out var container);
        var thickness = WidgetTheme.For(ui).SplitterSize;
        var pointer = ui.Input.Pointer;
        var state = ui.GetState<SplitState>(divider.Id);
        if (divider.Pressed && ui.TryGetRect(divider.Id, out var bar))
            state.Grab = horizontal ? pointer.X - bar.X : pointer.Y - bar.Y;
        else if (divider.Held && hasContainer)
            size = (horizontal ? pointer.X - container.X : pointer.Y - container.Y) - state.Grab;

        var extent = hasContainer ? (horizontal ? container.Width : container.Height) : float.PositiveInfinity;
        var shown = Math.Clamp(size, minFirst, MathF.Max(minFirst, extent - thickness - minSecond));
        if (divider.Held)
            size = shown;

        var scope = ui.Box(containerId, box with
        {
            Direction = direction,
            Width = box.Width ?? Sizing.Grow(),
            Height = box.Height ?? Sizing.Grow(),
        });
        return new SplitPane(ui, scope, divider, horizontal, shown);
    }
}
