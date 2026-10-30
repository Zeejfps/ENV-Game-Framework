using ZGF.Desktop;
using ZGF.KeyboardModule;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TextAreaTests
{
    // The test measurer: every character 8 wide, every line 16 tall.
    private const float CharWidth = 8f;
    private const float LineHeight = 16f;

    [Fact]
    public void Enter_submits_and_shift_enter_breaks_the_line()
    {
        var area = new Area();
        area.Click(0, 0);
        area.Type("one");

        area.Key(KeyboardKey.Enter, KeyModifiers.Shift);
        area.Type("two");
        Assert.Equal("one\ntwo", area.Text);
        Assert.Equal(0, area.Submits);

        area.Key(KeyboardKey.Enter);
        Assert.Equal("one\ntwo", area.Text);
        Assert.Equal(1, area.Submits);
    }

    [Fact]
    public void Without_submit_on_enter_enter_breaks_the_line_and_ctrl_enter_submits()
    {
        var area = new Area(submitOnEnter: false);
        area.Click(0, 0);
        area.Type("one");

        area.Key(KeyboardKey.Enter);
        Assert.Equal("one\n", area.Text);
        Assert.Equal(0, area.Submits);

        area.Key(KeyboardKey.Enter, KeyModifiers.Control);
        Assert.Equal(1, area.Submits);
    }

    [Fact]
    public void The_field_grows_a_line_at_a_time_up_to_its_most_lines_then_stops()
    {
        var area = new Area(maxLines: 3);
        var oneLine = area.Height;

        area.Text = "1\n2";
        area.Frame();
        Assert.Equal(oneLine + LineHeight, area.Height);

        area.Text = "1\n2\n3\n4\n5";
        area.Frame();
        Assert.Equal(oneLine + 2 * LineHeight, area.Height);
    }

    [Fact]
    public void A_long_line_wraps_between_words_and_grows_the_field()
    {
        var area = new Area(width: UiTheme.Dark.FieldPaddingX * 2 + 10.5f * CharWidth);
        var oneLine = area.Height;

        area.Text = "aaaa bbbb cccc";
        area.Frame(2);

        Assert.Equal(oneLine + LineHeight, area.Height);
        Assert.True(area.Frames.Drew("aaaa bbbb"));
        Assert.True(area.Frames.Drew("cccc"));
    }

    [Fact]
    public void Up_and_down_move_between_lines_keeping_the_caret_x()
    {
        var area = new Area("abcdef\nab\nabcdef");
        area.Click(0, 5);

        area.Key(KeyboardKey.DownArrow);
        area.Key(KeyboardKey.DownArrow);
        area.Type("|");

        Assert.Equal("abcdef\nab\nabcde|f", area.Text);
    }

    [Fact]
    public void Up_on_the_first_line_goes_to_the_start_and_down_on_the_last_to_the_end()
    {
        var area = new Area("abc\ndef");
        area.Click(0, 2);

        area.Key(KeyboardKey.UpArrow);
        area.Type("<");
        area.Key(KeyboardKey.DownArrow);
        area.Key(KeyboardKey.DownArrow);
        area.Type(">");

        Assert.Equal("<abc\ndef>", area.Text);
    }

    [Fact]
    public void Home_and_end_go_to_the_ends_of_the_line_and_with_ctrl_of_the_text()
    {
        var area = new Area("abc\ndef\nghi");
        area.Click(1, 1);

        area.Key(KeyboardKey.End);
        area.Type("]");
        area.Key(KeyboardKey.Home);
        area.Type("[");
        area.Key(KeyboardKey.End, KeyModifiers.Control);
        area.Type("$");

        Assert.Equal("abc\n[def]\nghi$", area.Text);
    }

    [Fact]
    public void Clicking_a_line_puts_the_caret_on_it()
    {
        var area = new Area("abc\ndef");

        area.Click(1, 2);
        area.Type("|");

        Assert.Equal("abc\nde|f", area.Text);
    }

    [Fact]
    public void Paste_keeps_line_breaks()
    {
        var area = new Area();
        area.Frames.Clipboard.Text = "x\r\ny";
        area.Click(0, 0);

        area.Key(KeyboardKey.V, KeyModifiers.Control);

        Assert.Equal("x\ny", area.Text);
    }

    [Fact]
    public void Typing_past_the_most_lines_scrolls_the_caret_into_view()
    {
        var area = new Area(maxLines: 2);
        area.Click(0, 0);

        for (var i = 0; i < 4; i++)
            area.Key(KeyboardKey.Enter, KeyModifiers.Shift);
        area.Type("end");
        area.Frame(2);

        var end = area.Frames.TextRect("end");
        var field = area.Frames.RectOf("area");
        Assert.Equal(field.Y + area.PaddingY + LineHeight, end.Y, 1f);
    }

    [Fact]
    public void A_disabled_area_stays_the_size_it_was()
    {
        var area = new Area("1\n2\n3", maxLines: 5);
        var enabledHeight = area.Height;

        area.Enabled = false;
        area.Frame();

        Assert.Equal(enabledHeight, area.Height);
        Assert.True(area.Frames.Drew("1") && area.Frames.Drew("3"));
    }

    [Fact]
    public void A_disabled_area_takes_no_typing_even_while_it_had_focus()
    {
        var area = new Area("bio");
        area.Click(0, 3);

        area.Enabled = false;
        area.Frame();
        area.Type("!");

        Assert.Equal("bio", area.Text);
    }

    private sealed class Area
    {
        private readonly float? _width;
        private readonly int _maxLines;
        private readonly bool _submitOnEnter;

        public Area(string text = "", float? width = null, int maxLines = 8, bool submitOnEnter = true)
        {
            Text = text;
            _width = width;
            _maxLines = maxLines;
            _submitOnEnter = submitOnEnter;
            Frames = new UiFrames(new DefaultUiState(), body: Body) { FrameTime = 1f / 60f };
            Frame(2);
        }

        public UiFrames Frames { get; }
        public string Text;
        public bool Enabled = true;
        public int Submits { get; private set; }
        public float Height => Frames.RectOf("area").Height;
        public float PaddingY => (UiTheme.Dark.ControlHeight - LineHeight) / 2;

        public void Frame(int count = 1) => Frames.Frame(count);

        private void Body(Ui ui)
        {
            using (ui.Screen())
            using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Fit(), Gap = 0f }))
            {
                var width = _width is { } w ? Sizing.Fixed(w) : (Sizing?)null;
                if (ui.TextArea("area", ref Text, width: width, maxLines: _maxLines, submitOnEnter: _submitOnEnter, enabled: Enabled))
                    Submits++;
            }
        }

        /// <summary>Clicks just right of character <paramref name="column"/>'s left edge on
        /// <paramref name="line"/>, as last laid out.</summary>
        public void Click(int line, int column)
        {
            var field = Frames.RectOf("area");
            var x = field.X + UiTheme.Dark.FieldPaddingX + column * CharWidth + 1f;
            var y = field.Y + PaddingY + line * LineHeight + LineHeight / 2;
            Frames.Wait(1).MovePointer(x, y).Press().Frame();
            Frames.Release().Frame();
        }

        public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => Frames.Key(key, modifiers).Frame();

        public void Type(string text) => Frames.Type(text).Frame();
    }
}
