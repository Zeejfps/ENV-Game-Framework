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

    // A 100-tall track laid out at a starting scale, ready for further Scale/position changes.
    private static VerticalScrollBarThumbView LaidOutThumb(float scale, float height = 100f)
    {
        var thumb = new VerticalScrollBarThumbView
        {
            LeftConstraint = 0f,
            BottomConstraint = 0f,
            WidthConstraint = 12f,
            HeightConstraint = height,
            Scale = scale,
        };
        thumb.LayoutSelf();
        return thumb;
    }

    // Regression: a bottom-pinned thumb must stay at the bottom when its size (Scale) later changes.
    // The position is the normalized fraction, not an absolute pixel offset — the old code stored the
    // pixel offset against the then-current travel, so a later resize re-mapped "bottom" to the middle.
    [Fact]
    public void SetScrollPositionNormalized_SurvivesScaleChange()
    {
        var thumb = LaidOutThumb(scale: 0.5f);
        thumb.SetScrollPositionNormalized(1f);

        thumb.Scale = 0.1f;
        thumb.LayoutSelf();

        Assert.Equal(0f, thumb.Position.Bottom, 3); // flush with the track bottom (BottomConstraint)
    }

    // Regression: layout must not echo a position back to the consumer. The thumb re-broadcasting from
    // OnLayoutSelf was the feedback loop that dragged the content to a stale fraction on a resize.
    [Fact]
    public void Layout_DoesNotEmitScrollPositionChanged()
    {
        var thumb = LaidOutThumb(scale: 0.5f);
        thumb.SetScrollPositionNormalized(1f);

        var fired = false;
        thumb.ScrollPositionChanged += _ => fired = true;

        thumb.Scale = 0.1f;
        thumb.LayoutSelf();

        Assert.False(fired);
    }

    // The flip side: user-driven moves still notify so the content follows the thumb.
    [Fact]
    public void Move_EmitsScrollPositionChanged()
    {
        var thumb = LaidOutThumb(scale: 0.5f);

        float? emitted = null;
        thumb.ScrollPositionChanged += n => emitted = n;
        thumb.Move(-25f); // drag down a quarter of the 50px travel

        Assert.NotNull(emitted);
        Assert.Equal(0.5f, emitted!.Value, 3);
    }
}
