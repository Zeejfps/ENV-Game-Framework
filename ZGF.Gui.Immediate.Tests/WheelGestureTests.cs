using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class WheelGestureTests
{
    private readonly UiFrames _frames = new(new DefaultUiState(), width: 400f, height: 300f, body: Page);

    private static void Page(Ui ui)
    {
        using var screen = ui.Screen();
        using (ui.Scroll(ui.GetId("page"), ScrollAxes.Vertical, new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }))
        {
            using (ui.Scroll(ui.GetId("row"), ScrollAxes.Horizontal, new Box { Width = Sizing.Grow(), Gap = 0f }))
            {
                for (var i = 0; i < 10; i++)
                    ui.Leaf(ui.GetId(i), new Box { Width = 100f, Height = 100f });
            }
            ui.Leaf(ui.GetId("below"), new Box { Width = 100f, Height = 1000f });
        }
    }

    private (float Across, float Down) Offsets() =>
        _frames.Frame(ui => (ui.ScrollOffset(ui.GetId("row")).X, ui.ScrollOffset(ui.GetId("page")).Y));

    private void Swipe(double down, double across) => _frames.Wait(0.016).Wheel(down, across).Frame(2);

    [Fact]
    public void A_sideways_swipe_over_a_row_leaves_the_page_where_it_is_for_the_whole_gesture()
    {
        _frames.Frame(2).MovePointer(50f, 50f).Frame();

        Swipe(-0.3, -1);
        Swipe(-1, -0.2);

        var (across, down) = Offsets();
        Assert.True(across > 0f);
        Assert.Equal(0f, down);
    }

    [Fact]
    public void A_downward_swipe_over_a_row_leaves_the_row_where_it_is_for_the_whole_gesture()
    {
        _frames.Frame(2).MovePointer(50f, 50f).Frame();

        Swipe(-1, -0.3);
        Swipe(-0.2, -1);

        var (across, down) = Offsets();
        Assert.Equal(0f, across);
        Assert.True(down > 0f);
    }

    [Fact]
    public void A_new_gesture_after_a_rest_picks_its_way_again()
    {
        _frames.Frame(2).MovePointer(50f, 50f).Frame();
        Swipe(-0.3, -1);
        var before = Offsets();

        _frames.Wait(1);
        Swipe(-1, -0.3);

        var after = Offsets();
        Assert.Equal(before.Across, after.Across);
        Assert.True(after.Down > before.Down);
    }
}
