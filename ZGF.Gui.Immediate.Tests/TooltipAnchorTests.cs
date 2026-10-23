using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TooltipAnchorTests
{
    private const float RowWidth = 300f;
    private const float RowHeight = 20f;

    private static UiFrames WideRow(TooltipAnchor anchor) =>
        new(body: ui =>
        {
            var row = ui.Interact("row");
            ui.Leaf(row.Id, new Box { Width = RowWidth, Height = RowHeight });
            ui.Tooltip(row.Id, "tip", anchor);
        }, width: 800f, height: 600f);

    private static UiRect ShowTipAt(UiFrames frames, float x)
    {
        frames.Frame();
        frames.MovePointer(x, 10f).Frame();
        frames.Wait(0.6).Frame(2);
        return frames.TextRect("tip");
    }

    [Fact]
    public void A_tooltip_anchored_to_the_pointer_starts_under_where_it_rests()
    {
        var tip = ShowTipAt(WideRow(TooltipAnchor.Pointer), 220f);

        Assert.InRange(tip.X, 220f, 240f);
        Assert.True(tip.Y > 10f + 16f);
    }

    [Fact]
    public void A_tooltip_anchored_to_its_element_shows_centered_under_it()
    {
        var tip = ShowTipAt(WideRow(TooltipAnchor.Element), 220f);

        Assert.Equal(RowWidth / 2, tip.X + tip.Width / 2, 1f);
        Assert.True(tip.Y > RowHeight);
    }

    [Fact]
    public void Once_shown_a_pointer_tooltip_stays_put_while_the_pointer_moves_on_the_row()
    {
        var frames = WideRow(TooltipAnchor.Pointer);
        var shown = ShowTipAt(frames, 100f);

        frames.MovePointer(180f, 12f).Frame(2);

        Assert.Equal(shown, frames.TextRect("tip"));
    }
}
