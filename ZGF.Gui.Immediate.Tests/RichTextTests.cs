using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Tests;

public sealed class RichTextTests
{
    private const uint Red = 0xFFFF0000u;
    private const uint Chip = 0xFF202020u;

    private static IEnumerable<string> Drawn(UiFrames frames) => frames.Canvas.Texts.Select(t => t.Inputs.Text);

    [Fact]
    public void Runs_wrap_together_between_words_and_the_element_is_as_tall_as_its_lines()
    {
        IReadOnlyList<RichTextRun>? runs = null;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            runs ??= [new RichTextRun("the quick ", ui.StyleText()), new RichTextRun("brown fox jumps", ui.StyleText(Red))];
            using (ui.Column(new Box { Width = 100f }))
                ui.RichText(runs, id: ui.GetId("t"));
        }).Frame();

        var rect = frames.RectOf("t");

        Assert.Equal(["the quick ", "brown fox ", "jumps"], Drawn(frames));
        Assert.Equal(100f, rect.Width);
        Assert.Equal(3 * 16f, rect.Height);
        Assert.Equal(Red, frames.Canvas.Texts.Single(t => t.Inputs.Text == "jumps").Inputs.Style.TextColor.Value);
    }

    [Fact]
    public void A_slice_starts_where_the_run_before_it_ends_on_its_line()
    {
        IReadOnlyList<RichTextRun>? runs = null;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            runs ??= [new RichTextRun("call ", ui.StyleText()), new RichTextRun("Run()", ui.StyleText(Red), IsCode: true)];
            ui.RichText(runs, codeBackground: Chip, wrap: false);
        }).Frame();

        var call = frames.TextRects().Single(t => t.Text == "Run()");
        var chip = frames.Canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == Chip);

        Assert.Equal(5 * 8f, call.Rect.X);
        Assert.Equal(5 * 8f, frames.ToUi(chip.Inputs.Position).X);
        Assert.Equal(5 * 8f, frames.ToUi(chip.Inputs.Position).Width);
    }

    /// <summary>"see " then a link to "list", in its own frames, keeping the last link clicked.</summary>
    private sealed class LinkText
    {
        private IReadOnlyList<RichTextRun>? _runs;

        public LinkText() => Frames = new UiFrames(new DefaultUiState(), body: Render);

        public UiFrames Frames { get; }
        public RichTextClick Clicked { get; private set; }

        private void Render(Ui ui)
        {
            _runs ??= [new RichTextRun("see ", ui.StyleText()), new RichTextRun("List", ui.StyleText(Red), Link: "list")];
            var click = ui.RichText(_runs, wrap: false, id: ui.GetId("t"));
            if (click.Link != null)
                Clicked = click;
        }
    }

    [Fact]
    public void A_link_under_the_pointer_shows_a_hand_and_an_underline()
    {
        var text = new LinkText();
        var frames = text.Frames.Frame();
        var link = frames.TextRect("List");
        var plain = frames.TextRect("see ");

        frames.MovePointer(plain).Frame(2);
        var plainCursor = frames.Cursor;
        var plainRules = frames.Canvas.Lines.Count;
        frames.MovePointer(link).Frame(2);

        Assert.NotEqual(MouseCursor.Hand, plainCursor);
        Assert.Equal(0, plainRules);
        Assert.Equal(MouseCursor.Hand, frames.Cursor);
        Assert.Single(frames.Canvas.Lines);
    }

    [Fact]
    public void A_left_or_middle_click_on_a_link_returns_it_with_its_button()
    {
        var text = new LinkText();
        var frames = text.Frames.Frame();
        var link = frames.TextRect("List");

        frames.Click(link).Frame();
        var left = text.Clicked;
        frames.Click(link, PointerButton.Middle).Frame();

        Assert.Equal(new RichTextClick("list", PointerButton.Left), left);
        Assert.Equal(new RichTextClick("list", PointerButton.Middle), text.Clicked);
    }

    [Fact]
    public void Without_wrap_the_runs_keep_their_lines_and_break_only_at_newlines()
    {
        IReadOnlyList<RichTextRun>? runs = null;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            runs ??= [new RichTextRun("one two three\nfour", ui.StyleText())];
            using (ui.Column(new Box { Width = 40f }))
                ui.RichText(runs, wrap: false, id: ui.GetId("t"));
        }).Frame();

        Assert.Equal(["one two three", "four"], Drawn(frames));
        Assert.Equal(2 * 16f, frames.RectOf("t").Height);
    }
}
