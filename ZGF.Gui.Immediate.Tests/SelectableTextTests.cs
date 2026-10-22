using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SelectableTextTests
{
    private const float CharWidth = 8f;
    private const float MiddleY = 10f;

    [Fact]
    public void Dragging_selects_the_characters_passed_over()
    {
        var label = new Label("hello world");

        label.Press(0);
        label.MoveTo(5);
        label.Release();

        Assert.Equal("hello", label.Selected);
    }

    [Fact]
    public void A_double_click_selects_a_word_and_a_triple_click_all()
    {
        var label = new Label("hello world");

        label.Click(8);
        label.Click(8, fresh: false);
        Assert.Equal("world", label.Selected);

        label.Click(8, fresh: false);
        Assert.Equal("hello world", label.Selected);
    }

    [Fact]
    public void A_click_selects_nothing()
    {
        var label = new Label("hello world");

        label.Click(3);

        Assert.False(label.Selection.HasSelection);
        Assert.Equal(3, label.Selection.Caret);
    }

    [Fact]
    public void An_inactive_text_ignores_the_pointer_and_is_shortened()
    {
        var label = new Label("hello wonderful world", width: 80f) { Active = false };

        label.Press(1);
        label.MoveTo(5);
        label.Release();

        Assert.False(label.Selection.HasSelection);
        Assert.Contains(label.Frames.Canvas.Texts, t => t.Inputs.Text.EndsWith('…'));
    }

    [Fact]
    public void Dragging_past_the_edge_scrolls_the_text_to_follow_the_caret()
    {
        var label = new Label("hello wonderful world", width: 80f);

        label.Press(0);
        for (var frame = 0; frame < 4; frame++)
            label.MoveTo(15);
        label.Release();

        Assert.Equal("hello wonderful world", label.Selected);
        Assert.Equal("hello wonderful world".Length * CharWidth - 80f, label.Shift);
    }

    [Fact]
    public void The_caret_lands_on_the_nearest_boundary()
    {
        var (before, after) = new UiFrames().Run(0f, ui => (ui.CaretAt("abc", CharWidth * 1.4f), ui.CaretAt("abc", CharWidth * 1.6f)));

        Assert.Equal((1, 2), (before, after));
    }

    [Fact]
    public void The_caret_never_lands_inside_a_grapheme_cluster()
    {
        var caret = new UiFrames().Run(0f, ui => ui.CaretAt("a👍🏽b", CharWidth * 3.6f));

        Assert.Equal(5, caret);
    }

    private sealed class Label
    {
        private readonly string _text;
        private readonly float _width;
        private float _shift;

        public Label(string text, float width = 200f)
        {
            _text = text;
            _width = width;
            Frames = new UiFrames(body: Body) { FrameTime = 1f / 60f };
            Frames.Frame();
        }

        public UiFrames Frames { get; }
        public TextSelection Selection { get; } = new();
        public bool Active { get; init; } = true;
        public float Shift => _shift;
        public string Selected => Selection.Of(_text);

        private void Body(Ui ui)
        {
            using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }))
            {
                var i = ui.Interact("label");
                using (ui.Row(i.Id, new Box { Width = _width, Height = MiddleY * 2, Gap = 0f }))
                    ui.SelectableText(i, _text, Selection, ref _shift, Active);
            }
        }

        public void Press(int index, bool fresh = true) => Frames.Wait(fresh ? 1 : 0).MovePointer(index * CharWidth, MiddleY).Press().Frame();

        public void MoveTo(int index) => Frames.MovePointer(index * CharWidth, MiddleY).Frame();

        public void Release() => Frames.Release().Frame();

        public void Click(int index, bool fresh = true)
        {
            Press(index, fresh);
            Release();
        }
    }
}

public sealed class SelectableTextAreaTests
{
    private const float CharWidth = 8f;
    private const float LineHeight = 20f;

    private readonly UiFrames _frames;
    private readonly TextSelection _selection = new();
    private float _shift;
    private const string Text = "one two\r\nthree four\nfive";

    public SelectableTextAreaTests()
    {
        _frames = new UiFrames(body: Body) { FrameTime = 1f / 60f };
        _frames.Frame();
    }

    private void Body(Ui ui)
    {
        using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }))
        {
            var i = ui.Interact("area");
            using (ui.Column(i.Id, new Box { Width = 200f, Height = LineHeight * 3, Gap = 0f }))
                ui.SelectableTextArea(i, Text, _selection, ref _shift, true, LineHeight);
        }
    }

    private UiFrames At(int column, int line) => _frames.MovePointer(column * CharWidth, line * LineHeight + LineHeight / 2);

    [Fact]
    public void Each_line_is_drawn_a_line_height_below_the_last_without_its_line_break()
    {
        Assert.True(_frames.Drew("one two"));
        Assert.True(_frames.Drew("three four"));
        Assert.True(_frames.Drew("five"));
    }

    [Fact]
    public void Dragging_from_one_line_to_another_selects_across_the_break()
    {
        _frames.Wait(1);
        At(4, 0).Press().Frame();
        At(5, 1).Frame();
        _frames.Release().Frame();

        Assert.Equal("two\r\nthree", _selection.Of(Text));
    }

    [Fact]
    public void A_double_click_on_a_later_line_selects_its_word()
    {
        _frames.Wait(1);
        At(1, 2);
        for (var click = 0; click < 2; click++)
        {
            _frames.Press().Frame();
            _frames.Release().Frame();
        }

        Assert.Equal("five", _selection.Of(Text));
    }
}

public sealed class SelectableLinkTests
{
    private const float CharWidth = 8f;
    private const string Glyph = "@";

    private readonly UiFrames _frames;
    private readonly TextSelection _selection = new();
    private float _shift;
    private bool _followed;

    public SelectableLinkTests()
    {
        _frames = new UiFrames(body: Body) { FrameTime = 1f / 60f };
        _frames.Frame();
    }

    private void Body(Ui ui)
    {
        using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }))
        {
            var i = ui.Interact("link");
            using (ui.Row(i.Id, new Box { Width = 200f, Height = 20f, Gap = 0f }))
                _followed |= ui.SelectableLink(i, "target", _selection, ref _shift, true, new UiIcon(Glyph), "Go");
        }
    }

    private void Click(float x, KeyModifiers modifiers = KeyModifiers.None) =>
        _frames.Wait(1).MovePointer(x, 10f).Frame().Click(x, 10f, modifiers: modifiers);

    [Fact]
    public void Its_icon_shows_only_while_it_is_hovered()
    {
        Assert.False(_frames.Drew(Glyph));

        _frames.MovePointer(CharWidth, 10f).Frame(2);

        Assert.True(_frames.Drew(Glyph));
    }

    [Fact]
    public void A_click_on_the_text_selects_it_without_following()
    {
        Click(CharWidth * 2);

        Assert.False(_followed);
    }

    [Fact]
    public void A_click_on_its_icon_follows_it()
    {
        _frames.MovePointer(CharWidth, 10f).Frame(2);
        var icon = _frames.TextRect(Glyph);

        Click(icon.X + icon.Width / 2);

        Assert.True(_followed);
    }

    [Fact]
    public void Ctrl_click_on_the_text_follows_it()
    {
        Click(CharWidth * 2, KeyModifiers.Control);

        Assert.True(_followed);
    }
}
