using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>A range slider over steps 1 to 20, 200 wide, so each step is 10 wide; the range starts at 5 to 10.</summary>
public sealed class RangeSliderTests
{
    private const float StepWidth = 10f;

    private int _start = 5;
    private int _end = 10;
    private readonly UiFrames _frames;

    public RangeSliderTests()
    {
        _frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            using (ui.Row(new Box { Width = 200f }))
                ui.RangeSlider("range", ref _start, ref _end, 1, 20);
        });
        _frames.Frame(2);
    }

    private UiRect Track => _frames.RectOf("range");

    private float Middle => Track.Y + Track.Height / 2f;

    // The x of the middle of step n on the track.
    private float At(float step) => Track.X + (step - 1f + 0.5f) * Track.Width / 20f;

    private void Drag(float from, float to)
    {
        _frames.MovePointer(from, Middle).Press().Frame();
        _frames.MovePointer(to, Middle).Frame();
        _frames.Release().Frame();
    }

    [Fact]
    public void Dragging_the_bar_slides_the_range_keeping_its_length()
    {
        Drag(At(7.5f), At(7.5f) + 3f * StepWidth);

        Assert.Equal((8, 13), (_start, _end));
    }

    [Fact]
    public void Sliding_stops_at_the_end_of_the_track()
    {
        Drag(At(7.5f), At(7.5f) + 30f * StepWidth);

        Assert.Equal((15, 20), (_start, _end));
    }

    [Fact]
    public void Dragging_an_end_moves_only_that_end_never_past_the_other()
    {
        var startEdge = Track.X + 4f * Track.Width / 20f + 2f;
        Drag(startEdge, startEdge - 2f * StepWidth);
        Assert.Equal((3, 10), (_start, _end));

        var endEdge = Track.X + 10f * Track.Width / 20f - 2f;
        Drag(endEdge, endEdge - 20f * StepWidth);
        Assert.Equal((3, 3), (_start, _end));
    }

    [Fact]
    public void A_press_on_the_track_moves_the_bar_there()
    {
        _frames.Click(At(16f), Middle).Frame();

        Assert.Equal((14, 19), (_start, _end));
    }

    [Fact]
    public void A_double_click_on_the_bar_takes_in_every_step()
    {
        _frames.DoubleClick(At(7.5f), Middle).Frame();

        Assert.Equal((1, 20), (_start, _end));
    }

    [Fact]
    public void Left_and_Right_slide_it_while_it_has_the_focus()
    {
        _frames.Key(KeyboardKey.Tab).Frame(2);
        _frames.Key(KeyboardKey.RightArrow).Frame();
        _frames.Key(KeyboardKey.RightArrow).Frame();

        Assert.Equal((7, 12), (_start, _end));
    }

    [Fact]
    public void It_numbers_its_start_and_end_inside_the_bar()
    {
        Assert.True(_frames.Drew("5"));
        Assert.True(_frames.Drew("10"));
    }
}
