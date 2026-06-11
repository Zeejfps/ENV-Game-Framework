using ZGF.Geometry;
using ZGF.Gui.Desktop.Components.VerticalScrollBar;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the ScrollBar widget contract: input is wired through KbmInput onto the track and
/// thumb for the mounted lifetime, and the hover tier drives the thumb's selection highlight.
/// </summary>
public class ScrollBarTests
{
    private static (View root, VerticalScrollBarThumbView thumb, InputSystem input) Build()
    {
        var input = new InputSystem();
        var ctx = new Context();
        ctx.AddService(input);
        var thumb = new VerticalScrollBarThumbView();
        var root = new ScrollBar { Thumb = thumb }.BuildView(ctx);
        return (root, thumb, input);
    }

    [Fact]
    public void Mount_WiresTrackAndThumb_UnmountClears()
    {
        var (root, thumb, input) = Build();

        Assert.Null(input.GetController(root));
        Assert.Null(input.GetController(thumb));

        root.Mount();
        Assert.NotNull(input.GetController(root));
        Assert.NotNull(input.GetController(thumb));

        root.Unmount();
        Assert.Null(input.GetController(root));
        Assert.Null(input.GetController(thumb));
    }

    [Fact]
    public void ThumbHover_TogglesSelection()
    {
        var (root, thumb, input) = Build();
        root.Mount();
        var controller = input.GetController(thumb)!;
        var mouse = new Mouse { Point = new PointF(0, 0) };

        var enter = new MouseEnterEvent { Mouse = mouse, Phase = EventPhase.Bubbling };
        controller.OnMouseEnter(ref enter);
        Assert.True(thumb.IsSelected);

        var exit = new MouseExitEvent { Mouse = mouse, Phase = EventPhase.Bubbling };
        controller.OnMouseExit(ref exit);
        Assert.False(thumb.IsSelected);
    }
}
