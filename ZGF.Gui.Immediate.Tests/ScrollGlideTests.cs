using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class ScrollGlideTests
{
    private static void Column(Ui ui)
    {
        using var screen = ui.Screen();
        using (ui.Scroll("column", new Box { Gap = 0f }))
        {
            for (var i = 0; i < 100; i++)
                ui.Leaf(ui.GetId(i), new Box { Width = 10f, Height = 30f });
        }
    }

    private static UiFrames Gliding()
    {
        var frames = new UiFrames(new DefaultUiState(), body: Column) { FrameTime = 1f / 60f };
        frames.State.Theme = frames.State.Theme with { ScrollGlide = Spring.Default };
        frames.Frame(2);
        frames.MovePointer(50f, 50f).Frame();
        return frames;
    }

    private static float Offset(UiFrames frames) => frames.Frame(ui => ui.ScrollOffset(ui.GetId("column")).Y);

    [Fact]
    public void A_turn_of_the_wheel_glides_to_where_it_would_have_jumped()
    {
        var frames = Gliding();

        frames.Wheel(-1).Frame();
        var first = Offset(frames);
        frames.Frame(120);

        Assert.InRange(first, 0.01f, 39.99f);
        Assert.Equal(40f, Offset(frames));
    }

    [Fact]
    public void Turns_during_a_glide_add_up()
    {
        var frames = Gliding();

        frames.Wheel(-1).Frame();
        frames.Wheel(-1).Frame();
        frames.Wheel(-1).Frame(120);

        Assert.Equal(120f, Offset(frames));
    }

    [Fact]
    public void ScrollTo_stops_a_glide_where_it_sends_the_scroll()
    {
        var frames = Gliding();

        frames.Wheel(-5).Frame();
        frames.Frame(ui => ui.ScrollTo(ui.GetId("column"), 0f, 10f));
        frames.Frame(120);

        Assert.Equal(10f, Offset(frames));
    }

    [Fact]
    public void A_glide_stops_at_the_end_of_the_content()
    {
        var frames = Gliding();
        var max = frames.Frame(ui => ui.ScrollMax(ui.GetId("column")).Y);

        frames.Wheel(-1000).Frame(240);

        Assert.Equal(max, Offset(frames));
    }
}
