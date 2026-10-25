using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class ShrinkSizingTests
{
    private const float Button = 20f;

    /// <summary>A row of <paramref name="width"/>: a title that gives up room first, down to 40, then
    /// controls that shrink after it: a picker sized for 100 that shrinks to 30, and a fixed button.</summary>
    private static UiFrames Row(float width)
    {
        return new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Row(new Box { Width = width, Gap = 0f }))
            {
                using (ui.Box(ui.GetId("title"), new Box { Width = Sizing.Grow(40f) }))
                    ui.Text("Theme title", new Box { Width = Sizing.Grow() }, overflow: TextOverflow.Ellipsis);
                using (ui.Row(ui.GetId("controls"), new Box { Width = Sizing.Shrink() with { Priority = 1 }, Gap = 0f }))
                {
                    ui.Leaf(ui.GetId("picker"), new Box { Width = Sizing.Shrink(30f, 100f), Height = 10f });
                    ui.Leaf(new Box { Width = Button, Height = 10f });
                }
            }
        }).Frame();
    }

    [Fact]
    public void With_room_to_spare_a_shrinkable_element_keeps_its_size_and_the_title_takes_the_rest()
    {
        var frames = Row(400f);

        Assert.Equal(100f, frames.RectOf("picker").Width);
        Assert.Equal(120f, frames.RectOf("controls").Width);
        Assert.Equal(280f, frames.RectOf("title").Width);
    }

    [Fact]
    public void A_short_row_shrinks_the_title_to_its_minimum_before_the_controls()
    {
        var frames = Row(140f);

        Assert.Equal(40f, frames.RectOf("title").Width);
        Assert.Equal(100f, frames.RectOf("controls").Width);
        Assert.Equal(80f, frames.RectOf("picker").Width);
    }

    [Fact]
    public void Shrinking_controls_keep_room_for_what_cannot_shrink()
    {
        var frames = Row(60f);

        Assert.Equal(50f, frames.RectOf("controls").Width);
        Assert.Equal(30f, frames.RectOf("picker").Width);
    }

    [Fact]
    public void A_select_button_that_shrinks_cuts_its_label_short_and_keeps_its_size_when_there_is_room()
    {
        var narrow = Picker(120f);
        var wide = Picker(400f);

        Assert.Equal(80f, narrow.RectOf("pick").Width);
        Assert.Contains(narrow.Canvas.Texts, t => t.Inputs.Text.EndsWith('…'));
        Assert.Equal(160f, wide.RectOf("pick").Width);
        Assert.True(wide.Drew("Lucide icons"));
    }

    [Fact]
    public void A_shrinkable_element_never_grows_past_its_maximum_however_wide_its_content()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Row(new Box { Width = 400f, Gap = 0f }))
            {
                using var menu = ui.SelectButton("pick", "A font with a very long name indeed", Sizing.Shrink(40f, 60f, 100f));
            }
        }).Frame();

        Assert.Equal(100f, frames.RectOf("pick").Width);
        Assert.Contains(frames.Canvas.Texts, t => t.Inputs.Text.EndsWith('…'));
    }

    private static UiFrames Picker(float width)
    {
        return new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Row(new Box { Width = width, Gap = 0f }))
            {
                ui.Leaf(new Box { Width = 40f, Height = 10f });
                using var menu = ui.SelectButton("pick", "Lucide icons", Sizing.Shrink(80f, 160f));
            }
        }).Frame();
    }
}
