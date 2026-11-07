using ZGF.Desktop.Input;

namespace ZGF.Gui.Immediate.Tests;

public sealed class ScrollPhaseTests
{
    private readonly UiFrames _frames = new(new DefaultUiState(), width: 400f, height: 300f, body: _ => { });

    private (bool Began, bool Coasting) Read() => _frames.Frame(ui => (ui.Input.ScrollBegan, ui.Input.ScrollCoasting));

    [Fact]
    public void A_swipe_begins_when_the_fingers_say_it_does_and_not_while_it_goes_on()
    {
        _frames.Swipe(-1, 0, ScrollPhase.Began);
        Assert.Equal((true, false), Read());

        _frames.Swipe(-1, 0, ScrollPhase.Changed);
        Assert.Equal((false, false), Read());
    }

    [Fact]
    public void The_coast_after_a_swipe_is_told_apart_and_begins_nothing()
    {
        _frames.Swipe(-1, 0, ScrollPhase.Changed);
        Read();

        _frames.Swipe(-0.5, 0, ScrollPhase.None, ScrollPhase.Began);
        Assert.Equal((false, true), Read());

        _frames.Swipe(-0.2, 0, ScrollPhase.None, ScrollPhase.Changed);
        Assert.Equal((false, true), Read());
    }

    [Fact]
    public void Fingers_scrolling_after_the_coast_begin_a_new_swipe_though_their_first_was_not_reported()
    {
        _frames.Swipe(-0.5, 0, ScrollPhase.None, ScrollPhase.Changed);
        Read();

        _frames.Swipe(-1, 0, ScrollPhase.Changed);
        Assert.Equal((true, false), Read());
    }

    [Fact]
    public void A_frame_where_the_fingers_cut_the_coast_short_begins_a_swipe_and_is_not_coasting()
    {
        _frames.Swipe(-0.2, 0, ScrollPhase.None, ScrollPhase.Changed).Swipe(1, 0, ScrollPhase.Changed);

        Assert.Equal((true, false), Read());
    }

    [Fact]
    public void A_wheel_neither_begins_nor_coasts()
    {
        _frames.Wheel(-1);

        Assert.Equal((false, false), Read());
    }
}
