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
