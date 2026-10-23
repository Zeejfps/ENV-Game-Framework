using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class PartTooltipTests
{
    private const float PartWidth = 50f;

    /// <summary>A 100 wide element split into two parts the test hit-tests itself, each with a
    /// tooltip naming it.</summary>
    private static UiFrames TwoParts() =>
        new(body: ui =>
        {
            var b = ui.Interact("b");
            ui.Leaf(b.Id, new Box { Width = 2 * PartWidth, Height = 30f });
            var part = PartUnder(ui, b.Id);
            var anchor = new UiRect((part - 1) * PartWidth, 0f, PartWidth, 30f);
            using var tooltip = ui.TooltipContent(b.Id, part, anchor);
            if (tooltip.IsOpen)
                ui.Text($"part {part}");
        }, width: 800f, height: 600f);

    private static int PartUnder(Ui ui, int id)
    {
        if (!ui.IsHovered(id))
            return 0;
        if (ui.Input.Pointer.X < PartWidth)
            return 1;
        return 2;
    }

    private static bool Shows(UiFrames frames, string text) => frames.Canvas.Texts.Any(t => t.Inputs.Text == text);

    [Fact]
    public void A_part_shows_its_tooltip_after_the_delay_and_another_part_waits_again()
    {
        var frames = TwoParts().Frame();
        frames.MovePointer(20f, 15f).Frame();
        var early = Shows(frames, "part 1");
        frames.Wait(0.6).Frame(2);
        var first = Shows(frames, "part 1");

        frames.MovePointer(80f, 15f).Frame();
        var movedEarly = Shows(frames, "part 2");
        frames.Wait(0.6).Frame(2);

        Assert.False(early);
        Assert.True(first);
        Assert.False(movedEarly);
        Assert.True(Shows(frames, "part 2"));
    }

    [Fact]
    public void Leaving_the_parts_closes_the_tooltip()
    {
        var frames = TwoParts().Frame();
        frames.MovePointer(20f, 15f).Frame();
        frames.Wait(0.6).Frame(2);

        frames.MovePointer(400f, 300f).Frame(2);

        Assert.False(Shows(frames, "part 1"));
    }
}
