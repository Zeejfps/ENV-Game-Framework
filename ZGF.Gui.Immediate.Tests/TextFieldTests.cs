using ZGF.Desktop;
using ZGF.KeyboardModule;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TextFieldTests
{
    [Fact]
    public void A_cell_field_draws_its_box_only_while_it_has_focus()
    {
        var text = "name";
        var frames = new UiFrames(body: ui =>
        {
            using var screen = ui.Screen();
            ui.TextField("cell", ref text, width: 120f, look: FieldLook.Cell);
        }).Frame();
        bool DrewBox() => frames.Canvas.Rects.Any(r => r.Inputs.Style.BackgroundColor == UiTheme.Dark.Field);

        Assert.False(DrewBox());
        frames.ClickOn("cell").Frame();

        Assert.True(DrewBox());
    }

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

    [Fact]
    public void A_composition_shows_at_the_caret_without_entering_the_text()
    {
        var field = new Field("ab");
        field.Click(1);

        field.Compose("xy");

        Assert.True(field.Frames.Drew("axyb"));
        Assert.Equal("ab", field.Text);

        field.Frames.Compose("");
        field.Type("Z");

        Assert.Equal("aZb", field.Text);
        Assert.True(field.Frames.Drew("aZb"));
    }

    [Fact]
    public void A_composition_replaces_the_selection()
    {
        var field = new Field("hello world");
        field.Click(11);
        for (var i = 0; i < 5; i++) field.Key(KeyboardKey.LeftArrow, KeyModifiers.Shift);

        field.Compose("x");

        Assert.Equal("hello ", field.Text);
        Assert.True(field.Frames.Drew("hello x"));
    }

    [Fact]
    public void The_ime_caret_follows_the_caret_inside_the_composition()
    {
        var field = new Field("ab");
        field.Click(1);
        Assert.Equal(TextLeft + 1 * CharWidth, field.Frames.ImeCaret?.X);

        field.Compose("xyz", caret: 2);

        Assert.Equal(TextLeft + 3 * CharWidth, field.Frames.ImeCaret?.X);
    }

    [Fact]
    public void Losing_focus_discards_the_composition()
    {
        var field = new Field("ab");
        field.Click(1);
        field.Compose("xy");

        field.Key(KeyboardKey.Escape);
        field.Frame();

        Assert.Equal(1, field.Frames.CompositionResets);
        Assert.Null(field.Frames.ImeCaret);
        Assert.Equal("ab", field.Text);
        Assert.False(field.Frames.Drew("axyb"));
    }

    [Fact]
    public void Pressing_in_the_field_discards_the_composition()
    {
        var field = new Field("ab");
        field.Click(1);
        field.Compose("xy");

        field.Click(0);

        Assert.Equal(1, field.Frames.CompositionResets);
        Assert.True(field.Frames.Drew("ab"));
        Assert.Equal("ab", field.Text);
    }

    [Fact]
    public void Ctrl_arrows_stop_at_punctuation_as_well_as_spaces()
    {
        var field = new Field("foo.bar  baz");
        field.Click(0);

        field.Key(KeyboardKey.RightArrow, KeyModifiers.Control);
        field.Type("|");

        Assert.Equal("foo|.bar  baz", field.Text);
    }

    [Fact]
    public void Arrows_and_delete_step_over_a_whole_grapheme_cluster()
    {
        var field = new Field("a👍🏽b");
        field.Click(6);

        field.Key(KeyboardKey.LeftArrow);
        field.Key(KeyboardKey.LeftArrow);
        field.Type("|");
        Assert.Equal("a|👍🏽b", field.Text);

        field.Key(KeyboardKey.Delete);
        Assert.Equal("a|b", field.Text);
    }

    private const float TextLeft = 24f;
    private const float CharWidth = 8f;

    private sealed class Field
    {
        private const float MiddleY = 31f;

        private string _text;

        public Field(string text = "")
        {
            _text = text;
            Frames = new UiFrames(body: Body) { FrameTime = 1f / 60f };
            Frame();
        }

        public UiFrames Frames { get; }
        public TestClipboard Clipboard => Frames.Clipboard;

        public string Text
        {
            get => _text;
            set => _text = value;
        }

        public void Frame() => Frames.Frame();

        private void Body(Ui ui)
        {
            using (ui.Screen())
                ui.TextField("field", ref _text);
        }

        public void Press(int index, KeyModifiers modifiers = KeyModifiers.None, bool fresh = true) =>
            Frames.Wait(fresh ? 1 : 0).MovePointer(TextLeft + index * CharWidth, MiddleY).Press(modifiers).Frame();

        public void MoveTo(int index) => Frames.MovePointer(TextLeft + index * CharWidth, MiddleY).Frame();

        public void Release() => Frames.Release().Frame();

        public void Click(int index, KeyModifiers modifiers = KeyModifiers.None)
        {
            Press(index, modifiers);
            Release();
        }

        public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => Frames.Key(key, modifiers).Frame();

        public void Type(string text) => Frames.Type(text).Frame();

        public void Compose(string text, int? caret = null) => Frames.Compose(text, caret).Frame();

        public string Selection()
        {
            Clipboard.Text = null;
            Key(KeyboardKey.C, KeyModifiers.Control);
            return Clipboard.Text ?? "";
        }
    }
}

public sealed class InlineEditTests
{
    private readonly UiFrames _frames;
    private readonly List<InlineEditResult> _results = new();
    private string _text = "Bookmark 1";
    private bool _begin = true;
    private bool _editing = true;

    public InlineEditTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using (ui.Row(new Box { Height = 28f, Gap = 0f }))
        {
            if (_editing)
            {
                var result = ui.InlineEdit("name", ref _text, _begin);
                _begin = false;
                if (result != InlineEditResult.Editing)
                {
                    _results.Add(result);
                    _editing = false;
                }
            }
            var other = ui.Interact("other");
            ui.Leaf(other.Id, new Box { Width = 100f, Height = 20f });
        }
    }

    [Fact]
    public void It_starts_with_the_whole_text_selected_so_typing_replaces_it_and_Enter_commits()
    {
        _frames.Frame(2);

        _frames.Type("Calls First").Frame(2);
        _frames.Key(KeyboardKey.Enter).Frame(2);

        Assert.Equal("Calls First", _text);
        Assert.Equal([InlineEditResult.Committed], _results);
    }

    [Fact]
    public void Escape_cancels()
    {
        _frames.Frame(2);
        _frames.Type("x").Frame(2);

        _frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.Equal([InlineEditResult.Cancelled], _results);
    }

    [Fact]
    public void Clicking_elsewhere_commits()
    {
        _frames.Frame(2);

        _frames.ClickOn("other").Frame(2);

        Assert.Equal([InlineEditResult.Committed], _results);
    }

    [Fact]
    public void It_is_one_line_tall_rather_than_a_control_tall()
    {
        _frames.Frame(2);

        var rect = _frames.RectOf("name");

        Assert.True(rect.Height < UiTheme.Dark.ControlHeight, $"{rect.Height}");
        Assert.True(rect.Height <= 28f);
    }
}
