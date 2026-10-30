using ZGF.Gui.Immediate.Widgets;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate.Tests;

public sealed class PointerMotionTests
{
    private readonly UiFrames _frames = new(new DefaultUiState(), body: static _ => { });

    [Fact]
    public void Motion_adds_up_every_move_of_a_frame_and_starts_again_the_next()
    {
        _frames.MovePointer(10f, 10f).Frame();

        var motion = _frames.MovePointer(14f, 7f).MovePointer(20f, 9f).Frame(ui => ui.Input.PointerMotion);
        var still = _frames.Frame(ui => ui.Input.PointerMotion);

        Assert.Equal(new PointF(10f, -1f), motion);
        Assert.Equal(default, still);
    }

    [Fact]
    public void Coming_in_from_off_screen_is_no_motion()
    {
        var motion = _frames.MovePointer(300f, 200f).Frame(ui => ui.Input.PointerMotion);

        Assert.Equal(default, motion);
    }

    [Fact]
    public void A_locked_pointer_hovers_and_presses_nothing()
    {
        var clicks = 0;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            if (ui.Button("Press", id: "press"))
                clicks++;
        }).Frame();
        var button = frames.RectOf("press");

        frames.LockPointer(true).Click(button);

        Assert.Equal(0, clicks);
        Assert.False(frames.Frame(ui => ui.IsHovered(ui.GetId("press"))));
    }

    [Fact]
    public void A_press_of_a_locked_pointer_keeps_the_keyboard_focus()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            var field = ui.GetId("field");
            ui.TabStop(field);
            ui.Leaf(field, new Box { Width = Sizing.Fixed(100f), Height = Sizing.Fixed(20f) });
        });
        frames.Frame(ui => ui.Focus(ui.GetId("field")));

        frames.LockPointer(true).Press().Frame().Release().Frame();

        Assert.True(frames.Frame(ui => ui.HasFocus(ui.GetId("field"))));
    }

    [Fact]
    public void A_lock_is_asked_for_only_by_the_frames_that_ask()
    {
        Assert.True(_frames.Frame(ui =>
        {
            ui.LockPointer();
            return ui.PointerLockRequested;
        }));
        Assert.False(_frames.Frame(ui => ui.PointerLockRequested));
    }
}
