using ZGF.Gui.Desktop.Components.VerticalScrollBar;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Widgets;

/// <summary>
/// Vertical scrollbar: track + draggable thumb, wired entirely through <see cref="KbmInput"/>
/// (thumb drag/hover, track click-to-scroll). The consumer supplies the thumb view — it is
/// the scrollbar's API surface (position, scale, <c>ScrollPositionChanged</c>) for whoever
/// syncs it with a scroll pane. <see cref="Style"/> colors the track and thumb; unset, it falls
/// back to <see cref="ScrollBarStyle.Default"/>.
/// </summary>
public sealed record ScrollBar : Widget
{
    public required VerticalScrollBarThumbView Thumb { get; init; }
    public Prop<ScrollBarStyle> Style { get; init; } = ScrollBarStyle.Default;

    protected override IWidget Build(Context ctx)
    {
        var thumb = Thumb;
        var hovered = false;
        var dragging = false;

        Style.Apply(ctx, thumb, static (t, s) =>
        {
            t.IdleBackgroundColor = s.ThumbIdleBackground;
            t.HoveredBackgroundColor = s.ThumbHoverBackground;
            t.BorderSize = s.ThumbBorderSize;
            t.BorderColor = s.ThumbBorder;
        });

        return new KbmInput
        {
            Width = 12,
            OnMouseButton = (ref MouseButtonEvent e) =>
            {
                if (e.Phase == EventPhase.Bubbling
                    && e.Button == MouseButton.Left
                    && e.State == InputState.Pressed)
                {
                    thumb.ScrollToPoint(e.Mouse.Point);
                    e.Consume();
                }
            },
            Child = new Box
            {
                Background = Style.Select(s => s.TrackBackground),
                BorderSize = Style.Select(s => s.TrackBorderSize),
                BorderColor = Style.Select(s => s.TrackBorder),
                Children =
                [
                    new KbmInput
                    {
                        OnDragStart = () =>
                        {
                            dragging = true;
                            thumb.IsSelected = true;
                        },
                        OnDrag = delta => thumb.Move(delta.Y),
                        OnDragEnd = () =>
                        {
                            dragging = false;
                            if (!hovered) thumb.IsSelected = false;
                        },
                        OnHoverEnter = () =>
                        {
                            hovered = true;
                            thumb.IsSelected = true;
                        },
                        OnHoverExit = () =>
                        {
                            hovered = false;
                            if (!dragging) thumb.IsSelected = false;
                        },
                        Child = new Raw { View = thumb },
                    },
                ],
            },
        };
    }
}
