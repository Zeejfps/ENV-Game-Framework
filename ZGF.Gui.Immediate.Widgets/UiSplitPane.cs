using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>Which pane of a <see cref="UiWidgets.SplitPane"/> the app's size belongs to; the other
/// takes the rest.</summary>
public enum SplitSide
{
    First,
    Second,
}

/// <summary>An open <see cref="UiWidgets.SplitPane"/>: record <see cref="First"/>, then
/// <see cref="Second"/>, each in its own scope, then dispose.</summary>
public readonly struct SplitPane : IDisposable
{
    private readonly Ui _ui;
    private readonly Ui.Scope _container;
    private readonly Interaction _divider;
    private readonly bool _horizontal;
    private readonly SplitSide _sized;
    private readonly float _size;

    internal SplitPane(Ui ui, Ui.Scope container, Interaction divider, bool horizontal, SplitSide sized, float size)
    {
        _ui = ui;
        _container = container;
        _divider = divider;
        _horizontal = horizontal;
        _sized = sized;
        _size = size;
    }

    /// <summary>The first pane — left or top — a clipped column.</summary>
    public Ui.Scope First() => Pane(SplitSide.First);

    /// <summary>Records the divider, then opens the second pane, a clipped column.</summary>
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
        return Pane(SplitSide.Second);
    }

    private Ui.Scope Pane(SplitSide side)
    {
        var main = side == _sized ? Sizing.Fixed(_size) : Sizing.Grow();
        return _ui.Column(_horizontal
            ? new Box { Width = main, Height = Sizing.Grow(), Clip = true }
            : new Box { Width = Sizing.Grow(), Height = main, Clip = true });
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
    /// the user drags. <paramref name="size"/> is the width or height of the <paramref name="sized"/>
    /// pane, owned by the app and written while dragging; the other pane takes the rest, so it is
    /// the one that follows the container's size. The size is shown kept between the panes'
    /// minimums. Fills the space it is given unless <paramref name="box"/> sizes it.
    /// </summary>
    public static SplitPane SplitPane(this Ui ui, string id, Direction direction, ref float size, float minFirst = 0f, float minSecond = 0f,
        Box box = default, SplitSide sized = SplitSide.First)
    {
        var horizontal = direction == Direction.Horizontal;
        var containerId = ui.GetId(id);
        var divider = ui.Interact(HashCode.Combine(containerId, "divider"));
        if (divider.Hovered || divider.Held)
            ui.SetCursor(horizontal ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);

        var hasContainer = ui.TryGetRect(containerId, out var container);
        var thickness = WidgetTheme.For(ui).SplitterSize;
        var extent = hasContainer ? (horizontal ? container.Width : container.Height) : float.PositiveInfinity;
        var pointer = horizontal ? ui.Input.Pointer.X : ui.Input.Pointer.Y;
        var state = ui.GetState<SplitState>(divider.Id);
        if (divider.Pressed && ui.TryGetRect(divider.Id, out var bar))
            state.Grab = pointer - (horizontal ? bar.X : bar.Y);
        else if (divider.Held && hasContainer)
        {
            var start = horizontal ? container.X : container.Y;
            var dividerAt = pointer - state.Grab;
            size = sized == SplitSide.First ? dividerAt - start : start + extent - dividerAt - thickness;
        }

        var (minSized, minOther) = sized == SplitSide.First ? (minFirst, minSecond) : (minSecond, minFirst);
        var shown = Math.Clamp(size, minSized, MathF.Max(minSized, extent - thickness - minOther));
        if (divider.Held)
            size = shown;

        var scope = ui.Box(containerId, box with
        {
            Direction = direction,
            Width = box.Width ?? Sizing.Grow(),
            Height = box.Height ?? Sizing.Grow(),
        });
        return new SplitPane(ui, scope, divider, horizontal, sized, shown);
    }
}
