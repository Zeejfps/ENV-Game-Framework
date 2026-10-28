using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MarkdownTests
{
    private const string Mono = "mono";
    private const string Italic = "italic";

    private static readonly IconGlyph CopyIcon = IconGlyph.FromFont("c", "icons");
    private static readonly IconGlyph CopiedIcon = IconGlyph.FromFont("k", "icons");
    private static readonly MarkdownLook Look = new(Mono, Italic, CopyIcon, CopiedIcon);

    /// <summary><see cref="Text"/> drawn as markdown, or as it is when <see cref="Plain"/>, in a column
    /// <see cref="Width"/> wide, in its own frames, keeping the last link clicked. While
    /// <see cref="FocusElsewhere"/>, something else takes focus each frame.</summary>
    private sealed class MarkdownView
    {
        public MarkdownView(string text, float width = 400f, bool plain = false)
        {
            Text = text;
            Width = width;
            Plain = plain;
            Frames = new UiFrames(new DefaultUiState(), body: Render);
        }

        public string Text { get; set; }
        public float Width { get; }
        public bool Plain { get; }
        public bool FocusElsewhere { get; set; }
        public UiFrames Frames { get; }
        public RichTextClick Clicked { get; private set; }

        private void Render(Ui ui)
        {
            if (FocusElsewhere)
                ui.Focus(ui.GetId("elsewhere"));
            using (ui.Column(new Box { Width = Width }))
            {
                if (Plain)
                {
                    ui.SelectableParagraph("md", Text);
                    return;
                }
                var click = ui.Markdown("md", Text, Look);
                if (click.Link != null)
                    Clicked = click;
            }
        }
    }

    /// <summary>The point <paramref name="chars"/> characters into the drawn text
    /// <paramref name="text"/>, a character being 8 wide, halfway down its line.</summary>
    private static PointF At(UiFrames frames, string text, int chars)
    {
        var rect = frames.TextRect(text);
        return new PointF(rect.X + chars * 8f + 1f, rect.Y + rect.Height / 2f);
    }

    /// <summary>What Ctrl+C puts on the clipboard.</summary>
    private static string? Copy(UiFrames frames)
    {
        frames.Clipboard.Text = null;
        frames.Key(KeyboardKey.C, KeyModifiers.Control).Frame();
        return frames.Clipboard.Text;
    }

    private static int SelectionRects(UiFrames frames) =>
        frames.Canvas.Rects.Count(r => r.Inputs.Style.BackgroundColor == new DefaultUiState().Theme.TextSelection);

    [Fact]
    public void Dragging_across_blocks_selects_from_the_press_to_the_pointer_and_copies_as_it_reads()
    {
        var frames = new MarkdownView("first **paragraph**\n\nsecond one").Frames.Frame();

        frames.Drag(At(frames, "first ", 0), At(frames, "second one", 6)).Release().Frame();
        var highlighted = SelectionRects(frames);

        Assert.Equal("first paragraph\nsecond", Copy(frames));
        Assert.Equal(2, highlighted);
    }

    [Fact]
    public void Letting_go_of_a_drag_over_a_link_selects_without_following_it()
    {
        var view = new MarkdownView("read the [docs](https://example.com/docs) first");
        var frames = view.Frames.Frame();

        frames.Drag(At(frames, "read the ", 0), At(frames, "docs", 2)).Release().Frame();

        Assert.Null(view.Clicked.Link);
        Assert.Equal("read the do", Copy(frames));
    }

    [Fact]
    public void A_drag_past_the_text_selects_to_the_nearest_leaf()
    {
        var frames = new MarkdownView("alpha beta\n\ngamma").Frames.Frame();
        var below = frames.TextRect("gamma");

        frames.Drag(At(frames, "alpha beta", 6), new PointF(below.Right + 50f, below.Bottom + 80f)).Release().Frame();

        Assert.Equal("beta\ngamma", Copy(frames));
    }

    [Fact]
    public void A_double_click_selects_a_word_and_a_triple_click_its_block()
    {
        var doubled = new MarkdownView("one two three\n\nnext").Frames.Frame();
        var tripled = new MarkdownView("one two three\n\nnext").Frames.Frame();
        var two = At(doubled, "one two three", 5);

        doubled.DoubleClick(two.X, two.Y).Frame();
        tripled.DoubleClick(two.X, two.Y).Press().Frame().Release().Frame();

        Assert.Equal("two", Copy(doubled));
        Assert.Equal("one two three", Copy(tripled));
    }

    [Fact]
    public void Shift_click_extends_the_selection_from_its_anchor()
    {
        var frames = new MarkdownView("alpha beta gamma").Frames.Frame();
        var start = At(frames, "alpha beta gamma", 6);
        var end = At(frames, "alpha beta gamma", 10);

        frames.Click(start.X, start.Y).Frame();
        frames.Click(end.X, end.Y, modifiers: KeyModifiers.Shift).Frame();

        Assert.Equal("beta", Copy(frames));
    }

    [Fact]
    public void Ctrl_A_selects_everything_and_table_cells_copy_as_tab_separated_rows()
    {
        var frames = new MarkdownView("Intro\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n- item").Frames.Frame();
        var intro = At(frames, "Intro", 1);

        frames.Click(intro.X, intro.Y).Frame();
        frames.Key(KeyboardKey.A, KeyModifiers.Control).Frame();

        Assert.Equal("Intro\nA\tB\n1\t2\nitem", Copy(frames));
    }

    [Fact]
    public void The_selection_clears_when_something_else_takes_focus()
    {
        var view = new MarkdownView("alpha beta");
        var frames = view.Frames.Frame();
        frames.Drag(At(frames, "alpha beta", 0), At(frames, "alpha beta", 5)).Release().Frame();
        var before = SelectionRects(frames);

        view.FocusElsewhere = true;
        frames.Frame(2);

        Assert.Equal(1, before);
        Assert.Equal(0, SelectionRects(frames));
    }

    [Fact]
    public void A_selection_ending_in_a_block_that_grows_is_dropped_and_one_before_it_kept()
    {
        var view = new MarkdownView("kept text\n\ngrowing");
        var frames = view.Frames.Frame();
        frames.Drag(At(frames, "kept text", 0), At(frames, "kept text", 4)).Release().Frame();

        view.Text = "kept text\n\ngrowing more";
        frames.Frame();
        var kept = Copy(frames);
        frames.Drag(At(frames, "kept text", 5), At(frames, "growing more", 3)).Release().Frame();
        view.Text = "kept text\n\ngrowing more still";
        frames.Frame();

        Assert.Equal("kept", kept);
        Assert.Equal(0, SelectionRects(frames));
    }

    [Fact]
    public void A_code_blocks_button_copies_its_code_and_shows_it_did()
    {
        var frames = new MarkdownView("```\nline one\nline two\n```").Frames.Frame();
        var button = frames.Canvas.Texts.Single(t => t.Inputs.Text == "c");

        frames.Click(frames.ToUi(button.Inputs.Position)).Frame();

        Assert.Equal("line one\nline two", frames.Clipboard.Text);
        Assert.True(frames.Drew("k"));
    }

    [Fact]
    public void A_code_blocks_button_without_a_language_sits_on_its_first_line()
    {
        var frames = new MarkdownView("```\nline one\nline two\n```").Frames.Frame();
        var button = frames.ToUi(frames.Canvas.Texts.Single(t => t.Inputs.Text == "c").Inputs.Position);
        var first = frames.TextRect("line one");

        Assert.True(button.X > first.Right);
        Assert.InRange(button.Y, first.Y - first.Height, first.Y + first.Height);
    }

    [Fact]
    public void A_code_blocks_button_with_a_language_sits_above_its_code_beside_the_language()
    {
        var frames = new MarkdownView("```bash\nline one\n```").Frames.Frame();
        var button = frames.Canvas.Texts.Single(t => t.Inputs.Text == "c");

        Assert.True(frames.ToUi(button.Inputs.Position).Y < frames.TextRect("line one").Y);
        Assert.True(frames.Drew("bash"));
    }

    /// <summary>The HTML Ctrl+C puts on the clipboard after a click on the drawn text
    /// <paramref name="text"/> and Ctrl+A in <paramref name="markdown"/>.</summary>
    private static string? CopyAllAsHtml(string markdown, string text)
    {
        var frames = new MarkdownView(markdown).Frames.Frame();
        var start = At(frames, text, 0);
        frames.Click(start.X, start.Y).Frame();
        frames.Key(KeyboardKey.A, KeyModifiers.Control).Frame();
        Copy(frames);
        return frames.Clipboard.Html;
    }

    [Fact]
    public void Copying_puts_the_selection_on_the_clipboard_as_html_too_with_its_inline_styles()
    {
        var html = CopyAllAsHtml("# Title\n\nSome **bold**, *slanted*, `code`, ~~gone~~ and [docs](https://example.com/a?b=1&c=2).", "Title");

        Assert.Equal(
            "<h1>Title</h1><p>Some <b>bold</b>, <i>slanted</i>, <code>code</code>, <s>gone</s> and <a href=\"https://example.com/a?b=1&amp;c=2\">docs</a>.</p>",
            html);
    }

    [Fact]
    public void Lists_quotes_tables_and_code_copy_as_their_elements()
    {
        var html = CopyAllAsHtml("1. one\n2. two\n   - inner\n\n> said\n\n| A | B |\n|---|---|\n| 1 | |\n\n```\na < b\n```", "one");

        Assert.Equal(
            "<ol><li><p>one</p></li><li><p>two</p><ul><li><p>inner</p></li></ul></li></ol>"
            + "<blockquote><p>said</p></blockquote>"
            + "<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td></td></tr></table>"
            + "<pre><code>a &lt; b</code></pre>",
            html);
    }

    [Fact]
    public void A_partial_selection_copies_as_html_only_what_it_covers_inside_the_elements_around_it()
    {
        var frames = new MarkdownView("- alpha beta\n- gamma").Frames.Frame();

        frames.Drag(At(frames, "alpha beta", 6), At(frames, "gamma", 3)).Release().Frame();
        Copy(frames);

        Assert.Equal("<ul><li><p>beta</p></li><li><p>gam</p></li></ul>", frames.Clipboard.Html);
        Assert.Equal("beta\ngam", frames.Clipboard.Text);
    }

    [Fact]
    public void Part_of_a_numbered_list_copies_as_html_numbered_as_it_shows()
    {
        var frames = new MarkdownView("1. one\n2. two\n3. three").Frames.Frame();

        frames.Drag(At(frames, "two", 0), At(frames, "three", 5)).Release().Frame();
        Copy(frames);

        Assert.Equal("<ol start=\"2\"><li><p>two</p></li><li><p>three</p></li></ol>", frames.Clipboard.Html);
    }

    [Fact]
    public void A_code_block_keeps_the_blank_lines_before_its_closing_fence()
    {
        var frames = new MarkdownView("```\nx\n\n```").Frames.Frame();

        frames.Click(frames.ToUi(frames.Canvas.Texts.Single(t => t.Inputs.Text == "c").Inputs.Position)).Frame();

        Assert.Equal("x\n", frames.Clipboard.Text);
    }

    [Fact]
    public void Only_web_and_mail_links_keep_their_address_in_copied_html()
    {
        var html = CopyAllAsHtml("[run](file:///C:/tool.exe) [mail](mailto:a@b.c)", "run");

        Assert.Equal("<p>run <a href=\"mailto:a@b.c\">mail</a></p>", html);
    }

    [Fact]
    public void A_selectable_paragraph_shows_its_text_as_it_is_and_copies_it()
    {
        var frames = new MarkdownView("**not bold**\nnext line", plain: true).Frames.Frame();
        var start = At(frames, "**not bold**", 0);

        frames.Click(start.X, start.Y).Frame();
        frames.Key(KeyboardKey.A, KeyModifiers.Control).Frame();

        Assert.True(frames.Drew("next line"));
        Assert.Equal("**not bold**\nnext line", Copy(frames));
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
