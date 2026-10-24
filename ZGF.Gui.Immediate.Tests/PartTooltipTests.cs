using ZGF.KeyboardModule;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class PartTooltipTests
{
    private const float PartWidth = 50f;

    /// <summary>A 100 wide element split into two parts the test hit-tests itself, each with a
    /// tooltip naming it.</summary>
    private static UiFrames TwoParts() =>
        new(new DefaultUiState(), body: ui =>
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

public sealed class InteractiveTooltipTests
{
    private const float PartWidth = 50f;
    private const float PartHeight = 30f;

    /// <summary>A 100 wide element whose left half shows an interactive tooltip holding a link,
    /// keeping the last link clicked.</summary>
    private sealed class LinkTooltip
    {
        private static readonly TooltipLook Look = new(200f, new Spacing(4f), Align.Start, Interactive: true);
        private IReadOnlyList<RichTextRun>? _runs;

        public LinkTooltip() => Frames = new UiFrames(new DefaultUiState(), body: Render, width: 800f, height: 600f);

        public UiFrames Frames { get; }
        public RichTextClick Clicked { get; private set; }

        private void Render(Ui ui)
        {
            var b = ui.Interact("b");
            ui.Leaf(b.Id, new Box { Width = 2 * PartWidth, Height = PartHeight });
            var part = ui.IsHovered(b.Id) && ui.Input.Pointer.X < PartWidth ? 1 : 0;
            var anchor = new UiRect(0f, 0f, PartWidth, PartHeight);
            using var tooltip = ui.TooltipContent(b.Id, part, anchor, out var shown, Look);
            if (!tooltip.IsOpen || shown != 1)
                return;
            _runs ??= [new RichTextRun("go to ", ui.StyleText()), new RichTextRun("target", ui.StyleText(), Link: "target")];
            var click = ui.RichText(_runs, wrap: false, id: ui.GetId("link"));
            if (click.Link != null)
                Clicked = click;
        }
    }

    private static UiFrames Opened(LinkTooltip tooltip)
    {
        var frames = tooltip.Frames.Frame();
        frames.MovePointer(20f, 15f).Frame();
        frames.Wait(0.6).Frame(2);
        return frames;
    }

    private static bool Shows(UiFrames frames) => frames.Drew("target");

    [Fact]
    public void The_pointer_can_move_from_the_part_into_the_tooltip_and_it_stays_open()
    {
        var frames = Opened(new LinkTooltip());
        var link = frames.TextRect("target");

        frames.MovePointer(link).Frame();
        frames.Wait(1.0).Frame(2);

        Assert.True(Shows(frames));
    }

    [Fact]
    public void Leaving_for_elsewhere_closes_it_after_a_moment()
    {
        var frames = Opened(new LinkTooltip());

        frames.MovePointer(500f, 400f).Frame();
        var during = Shows(frames);
        frames.Wait(0.3).Frame(2);

        Assert.True(during);
        Assert.False(Shows(frames));
    }

    [Fact]
    public void Escape_closes_it()
    {
        var frames = Opened(new LinkTooltip());

        frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.False(Shows(frames));
    }

    [Fact]
    public void A_link_in_it_can_be_clicked()
    {
        var tooltip = new LinkTooltip();
        var frames = Opened(tooltip);
        var link = frames.TextRect("target");

        frames.MovePointer(link).Frame();
        frames.Click(link).Frame();

        Assert.Equal(new RichTextClick("target", PointerButton.Left), tooltip.Clicked);
    }

    [Fact]
    public void Content_taller_than_the_look_allows_scrolls_inside_it()
    {
        var look = new TooltipLook(200f, new Spacing(4f), Align.Start, Interactive: true, MaxHeight: 60f);
        var lines = string.Join('\n', Enumerable.Range(0, 20).Select(i => $"line {i}"));
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            var b = ui.Interact("b");
            ui.Leaf(b.Id, new Box { Width = PartWidth, Height = PartHeight });
            var part = ui.IsHovered(b.Id) ? 1 : 0;
            using var tooltip = ui.TooltipContent(b.Id, part, new UiRect(0f, 0f, PartWidth, PartHeight), look);
            if (tooltip.IsOpen)
                ui.Text(lines);
        }, width: 800f, height: 600f).Frame();
        frames.MovePointer(20f, 15f).Frame();
        frames.Wait(0.6).Frame(2);

        Assert.True(frames.Drew("line 0"));
        Assert.False(frames.Drew("line 19"));
    }
}
