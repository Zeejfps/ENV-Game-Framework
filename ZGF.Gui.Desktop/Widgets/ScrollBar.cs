using ZGF.Gui.Desktop.Components.VerticalScrollBar;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Widgets;

/// <summary>
/// Vertical scrollbar: track + draggable thumb, wired entirely through <see cref="KbmInput"/>
/// (thumb drag/hover, track click-to-scroll). The consumer supplies the thumb view — it is
/// the scrollbar's API surface (position, scale, <c>ScrollPositionChanged</c>) for whoever
/// syncs it with a scroll pane.
/// </summary>
public sealed record ScrollBar : Widget
{
    public required VerticalScrollBarThumbView Thumb { get; init; }

    protected override IWidget Build(Context ctx)
    {
        var thumb = Thumb;
        var hovered = false;
        var dragging = false;
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
                Background = 0xFFCECECE,
                BorderSize = BorderSizeStyle.All(1),
                BorderColor = new BorderColorStyle
                {
                    Left = 0xFF9C9C9C,
                    Top = 0xFF9C9C9C,
                    Right = 0xFFFFFFFF,
                    Bottom = 0xFFFFFFFF,
                },
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
