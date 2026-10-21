using ZGF.Desktop;
using ZGF.KeyboardModule;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TextFieldTests
{
    [Fact]
    public void Typing_inserts_at_the_caret_and_replaces_a_selection()
    {
        var field = new Field();
        field.Click(0);
        field.Type("hello world");
        for (var i = 0; i < 5; i++) field.Key(KeyboardKey.LeftArrow, KeyModifiers.Shift);

        field.Type("there");

        Assert.Equal("hello there", field.Text);
    }

    [Fact]
    public void A_plain_arrow_collapses_the_selection_to_its_edge()
    {
        var field = new Field("hello world");
        field.Click(11);
        for (var i = 0; i < 5; i++) field.Key(KeyboardKey.LeftArrow, KeyModifiers.Shift);
        Assert.Equal("world", field.Selection());

        field.Key(KeyboardKey.LeftArrow);
        field.Type("X");

        Assert.Equal("hello Xworld", field.Text);
    }

    [Fact]
    public void Ctrl_arrows_move_by_word_and_shift_extends()
    {
        var field = new Field("one two  three");
        field.Click(0);

        field.Key(KeyboardKey.RightArrow, KeyModifiers.Control);
        field.Key(KeyboardKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal("two  ", field.Selection());

        field.Key(KeyboardKey.End);
        field.Key(KeyboardKey.LeftArrow, KeyModifiers.Control);
        field.Type("|");
        Assert.Equal("one two  |three", field.Text);
    }

    [Fact]
    public void Ctrl_backspace_and_ctrl_delete_remove_whole_words()
    {
        var field = new Field("one two three");
        field.Click(13);

        field.Key(KeyboardKey.Backspace, KeyModifiers.Control);
        Assert.Equal("one two ", field.Text);
        field.Key(KeyboardKey.Backspace, KeyModifiers.Control);
        Assert.Equal("one ", field.Text);

        field.Key(KeyboardKey.Home);
        field.Key(KeyboardKey.Delete, KeyModifiers.Control);
        Assert.Equal("", field.Text);
    }

    [Fact]
    public void Select_all_copy_cut_and_paste()
    {
        var field = new Field("hello");
        field.Click(0);

        field.Key(KeyboardKey.A, KeyModifiers.Control);
        Assert.Equal("hello", field.Selection());

        field.Key(KeyboardKey.X, KeyModifiers.Control);
        Assert.Equal("", field.Text);
        Assert.Equal("hello", field.Clipboard.Text);

        field.Key(KeyboardKey.V, KeyModifiers.Control);
        field.Key(KeyboardKey.V, KeyModifiers.Control);
        Assert.Equal("hellohello", field.Text);
    }

    [Fact]
    public void Paste_replaces_the_selection_and_flattens_line_breaks()
    {
        var field = new Field("abc");
        field.Clipboard.Text = "x\ny";
        field.Click(3);
        field.Key(KeyboardKey.LeftArrow, KeyModifiers.Shift);

        field.Key(KeyboardKey.V, KeyModifiers.Control);

        Assert.Equal("abx y", field.Text);
    }

    [Fact]
    public void Undo_merges_a_run_of_typing_and_redo_replays_it()
    {
        var field = new Field();
        field.Click(0);
        field.Type("hel");
        field.Type("lo");
        field.Key(KeyboardKey.LeftArrow);
        field.Type("X");
        Assert.Equal("hellXo", field.Text);

        field.Key(KeyboardKey.Z, KeyModifiers.Control);
        Assert.Equal("hello", field.Text);
        field.Key(KeyboardKey.Z, KeyModifiers.Control);
        Assert.Equal("", field.Text);

        field.Key(KeyboardKey.Y, KeyModifiers.Control);
        Assert.Equal("hello", field.Text);
        field.Key(KeyboardKey.Z, KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal("hellXo", field.Text);
    }

    [Fact]
    public void Undo_brings_back_the_selection_that_was_replaced()
    {
        var field = new Field("hello world");
        field.Click(11);
        for (var i = 0; i < 5; i++) field.Key(KeyboardKey.LeftArrow, KeyModifiers.Shift);
        field.Type("there");

        field.Key(KeyboardKey.Z, KeyModifiers.Control);

        Assert.Equal("hello world", field.Text);
        Assert.Equal("world", field.Selection());
    }

    [Fact]
    public void Changing_the_text_from_outside_forgets_the_undo_history()
    {
        var field = new Field();
        field.Click(0);
        field.Type("abc");

        field.Text = "";
        field.Frame();
        field.Key(KeyboardKey.Z, KeyModifiers.Control);

        Assert.Equal("", field.Text);
    }

    [Fact]
    public void Dragging_selects_from_the_press_to_the_pointer()
    {
        var field = new Field("hello world");

        field.Press(2);
        field.MoveTo(7);
        field.Release();

        Assert.Equal("llo w", field.Selection());
    }

    [Fact]
    public void Double_click_selects_a_word_and_dragging_extends_by_words()
    {
        var field = new Field("one two three");
        field.Press(5);
        field.Release();

        field.Press(5, fresh: false);
        Assert.Equal("two", field.Selection());

        field.MoveTo(10);
        field.Release();
        Assert.Equal("two three", field.Selection());
    }

    [Fact]
    public void Triple_click_selects_everything()
    {
        var field = new Field("one two three");
        field.Press(5);
        field.Release();
        field.Press(5, fresh: false);
        field.Release();
        field.Press(5, fresh: false);
        field.Release();

        Assert.Equal("one two three", field.Selection());
    }

    [Fact]
    public void Shift_click_extends_the_selection_from_the_caret()
    {
        var field = new Field("hello world");
        field.Click(0);

        field.Click(5, KeyModifiers.Shift);

        Assert.Equal("hello", field.Selection());
    }

    [Fact]
    public void The_caret_blinks_on_a_timer_instead_of_redrawing_every_frame()
    {
        var field = new Field("hello");
        field.Click(5);
        field.Frame();

        Assert.False(field.Frames.RequestedFrame);
        Assert.InRange(field.Frames.WakeAt, field.Frames.RealTime, field.Frames.RealTime + 0.53);
    }

    [Theory]
    [InlineData("foo.bar  baz", 1, 0, 3)]
    [InlineData("foo.bar  baz", 3, 3, 4)]
    [InlineData("foo.bar  baz", 8, 7, 9)]
    [InlineData("foo.bar  baz", 12, 9, 12)]
    public void A_word_is_a_run_of_one_kind_of_character(string text, int index, int start, int end)
    {
        Assert.Equal((start, end), UiWidgets.WordAt(text, index));
    }

    [Theory]
    [InlineData("foo.bar  baz", 12, 9)]
    [InlineData("foo.bar  baz", 9, 4)]
    [InlineData("foo.bar  baz", 4, 3)]
    [InlineData("foo.bar  baz", 3, 0)]
    public void Previous_word_skips_spaces_then_one_run(string text, int index, int expected)
    {
        Assert.Equal(expected, UiWidgets.PreviousWord(text, index));
    }

    [Theory]
    [InlineData("foo.bar  baz", 0, 3)]
    [InlineData("foo.bar  baz", 4, 9)]
    [InlineData("foo.bar  baz", 7, 9)]
    [InlineData("foo.bar  baz", 9, 12)]
    public void Next_word_skips_one_run_then_spaces(string text, int index, int expected)
    {
        Assert.Equal(expected, UiWidgets.NextWord(text, index));
    }

    private sealed class Field
    {
        private const float TextLeft = 24f;
        private const float CharWidth = 8f;
        private const float MiddleY = 31f;

        private string _text;

        public Field(string text = "")
        {
            _text = text;
            Frame();
        }

        public UiFrames Frames { get; } = new();
        public TestClipboard Clipboard => Frames.Clipboard;

        public string Text
        {
            get => _text;
            set => _text = value;
        }

        public void Frame() => Frames.Run(1f / 60f, ui => { ui.TextField("field", ref _text); });

        public void Press(int index, KeyModifiers modifiers = KeyModifiers.None, bool fresh = true)
        {
            if (fresh) Frames.Wait(1);
            Frames.MovePointer(TextLeft + index * CharWidth, MiddleY);
            Frames.Press(modifiers);
            Frame();
        }

        public void MoveTo(int index)
        {
            Frames.MovePointer(TextLeft + index * CharWidth, MiddleY);
            Frame();
        }

        public void Release()
        {
            Frames.Release();
            Frame();
        }

        public void Click(int index, KeyModifiers modifiers = KeyModifiers.None)
        {
            Press(index, modifiers);
            Release();
        }

        public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None)
        {
            Frames.Key(key, modifiers);
            Frame();
        }

        public void Type(string text)
        {
            Frames.Type(text);
            Frame();
        }

        public string Selection()
        {
            Clipboard.Text = null;
            Key(KeyboardKey.C, KeyModifiers.Control);
            return Clipboard.Text ?? "";
        }
    }
}
