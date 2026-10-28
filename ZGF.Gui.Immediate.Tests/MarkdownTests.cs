using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MarkdownTests
{
    private const string Mono = "mono";
    private const string Italic = "italic";

    private static readonly MarkdownFonts Fonts = new(Mono, Italic);

    /// <summary><see cref="Text"/> drawn as markdown in a column <see cref="Width"/> wide, in its own
    /// frames, keeping the last link clicked.</summary>
    private sealed class MarkdownView
    {
        public MarkdownView(string text, float width = 400f)
        {
            Text = text;
            Width = width;
            Frames = new UiFrames(new DefaultUiState(), body: Render);
        }

        public string Text { get; set; }
        public float Width { get; }
        public UiFrames Frames { get; }
        public RichTextClick Clicked { get; private set; }

        private void Render(Ui ui)
        {
            using (ui.Column(new Box { Width = Width }))
            {
                var click = ui.Markdown("md", Text, Fonts);
                if (click.Link != null)
                    Clicked = click;
            }
        }
    }

    private static TextStyle StyleOf(UiFrames frames, string text) =>
        frames.Canvas.Texts.Single(t => t.Inputs.Text == text).Inputs.Style;

    [Fact]
    public void Inline_markup_styles_the_text_it_wraps()
    {
        var frames = new MarkdownView("plain **bold** *slanted* `code` ~~gone~~").Frames.Frame();

        Assert.Equal(FontWeight.Bold, StyleOf(frames, "bold").FontWeight.Value);
        Assert.Equal(Italic, StyleOf(frames, "slanted").FontFamily.Value);
        Assert.Equal(Mono, StyleOf(frames, "code").FontFamily.Value);
        Assert.True(frames.Drew("plain "));
        Assert.True(frames.Drew("gone"));
        Assert.DoesNotContain(frames.Canvas.Texts, t => t.Inputs.Text.Contains('*') || t.Inputs.Text.Contains('`'));
    }

    [Fact]
    public void Headings_are_bold_and_the_first_levels_larger_than_body_text()
    {
        var frames = new MarkdownView("# Title\n\n## Section\n\n### Part\n\nbody").Frames.Frame();
        var theme = new DefaultUiState().Theme;

        Assert.True(StyleOf(frames, "Title").FontSize.Value > theme.FontSizeTitle);
        Assert.Equal(theme.FontSizeTitle, StyleOf(frames, "Section").FontSize.Value);
        Assert.Equal(theme.FontSize, StyleOf(frames, "Part").FontSize.Value);
        Assert.Equal(FontWeight.Bold, StyleOf(frames, "Part").FontWeight.Value);
        Assert.NotEqual(FontWeight.Bold, StyleOf(frames, "body").FontWeight.Value);
    }

    [Fact]
    public void Blocks_stack_in_order_down_the_column()
    {
        var frames = new MarkdownView("first\n\nsecond\n\n---\n\nthird").Frames.Frame();

        var first = frames.TextRect("first");
        var second = frames.TextRect("second");
        var third = frames.TextRect("third");

        Assert.True(first.Y < second.Y);
        Assert.True(second.Y < third.Y);
        Assert.Equal(first.X, third.X);
    }

    [Fact]
    public void A_code_block_keeps_its_lines_in_the_code_font_under_its_language()
    {
        var frames = new MarkdownView("```cs\nvar answer = Compute(alpha, beta, gamma);\nreturn answer;\n```", width: 120f).Frames.Frame();

        Assert.Equal(Mono, StyleOf(frames, "var answer = Compute(alpha, beta, gamma);").FontFamily.Value);
        Assert.True(frames.Drew("return answer;"));
        Assert.True(frames.TextRect("cs").Y < frames.TextRect("return answer;").Y);
    }

    [Fact]
    public void List_items_follow_their_markers_and_line_up()
    {
        var frames = new MarkdownView("1. one\n2. two\n\n- dot").Frames.Frame();

        var one = frames.TextRect("one");
        var two = frames.TextRect("two");

        Assert.True(frames.TextRect("1.").X < one.X);
        Assert.True(frames.TextRect("2.").X < two.X);
        Assert.Equal(one.X, two.X);
        Assert.True(frames.TextRect("•").X < frames.TextRect("dot").X);
    }

    [Fact]
    public void A_quote_is_indented_and_muted()
    {
        var frames = new MarkdownView("said\n\n> quoted").Frames.Frame();
        var theme = new DefaultUiState().Theme;

        Assert.True(frames.TextRect("quoted").X > frames.TextRect("said").X);
        Assert.Equal(theme.TextMuted, frames.TextColor("quoted"));
    }

    [Fact]
    public void Table_cells_line_up_in_rows_and_columns()
    {
        var frames = new MarkdownView("| Name | Size |\n|---|---:|\n| alpha | 1 |\n| b | 200 |").Frames.Frame();

        var name = frames.TextRect("Name");
        var alpha = frames.TextRect("alpha");
        var b = frames.TextRect("b");
        var one = frames.TextRect("1");
        var twoHundred = frames.TextRect("200");

        Assert.Equal(FontWeight.Bold, StyleOf(frames, "Name").FontWeight.Value);
        Assert.Equal(name.X, alpha.X);
        Assert.Equal(alpha.X, b.X);
        Assert.Equal(alpha.Y, one.Y);
        Assert.Equal(b.Y, twoHundred.Y);
        Assert.Equal(one.X + one.Width, twoHundred.X + twoHundred.Width);
    }

    [Fact]
    public void A_click_on_a_link_returns_its_url()
    {
        var view = new MarkdownView("see [the docs](https://example.com/docs) now");
        var frames = view.Frames.Frame();

        frames.Click(frames.TextRect("the docs")).Frame();

        Assert.Equal(new RichTextClick("https://example.com/docs", PointerButton.Left), view.Clicked);
    }

    [Fact]
    public void Text_that_grows_shows_what_it_grew_by()
    {
        var view = new MarkdownView("intro\n\n```\npartial");
        var frames = view.Frames.Frame();
        var partial = frames.Drew("partial");

        view.Text = "intro\n\n```\npartial line\n```\n\noutro";
        frames.Frame();

        Assert.True(partial);
        Assert.True(frames.Drew("intro"));
        Assert.True(frames.Drew("partial line"));
        Assert.True(frames.Drew("outro"));
        Assert.False(frames.Drew("partial"));
    }

    [Fact]
    public void Unsupported_syntax_shows_as_its_text()
    {
        var frames = new MarkdownView("<b>raw</b>").Frames.Frame();

        Assert.True(frames.Drew("<b>raw</b>"));
    }
}
