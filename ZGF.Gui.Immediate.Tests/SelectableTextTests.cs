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
        var frames = new UiFrames();

        var (before, after) = frames.Run(0f, ui => (ui.CaretAt("abc", CharWidth * 1.4f), ui.CaretAt("abc", CharWidth * 1.6f)));

        Assert.Equal((1, 2), (before, after));
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
            Frame();
        }

        public UiFrames Frames { get; } = new();
        public TextSelection Selection { get; } = new();
        public bool Active { get; init; } = true;
        public float Shift => _shift;
        public string Selected => Selection.Of(_text);

        private void Frame() => Frames.Run(1f / 60f, ui =>
        {
            using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }))
            {
                var i = ui.Interact("label");
                using (ui.Row(i.Id, new Box { Width = _width, Height = MiddleY * 2, Gap = 0f }))
                    ui.SelectableText(i, _text, Selection, ref _shift, Active);
            }
        });

        public void Press(int index, bool fresh = true)
        {
            if (fresh) Frames.Wait(1);
            Frames.MovePointer(index * CharWidth, MiddleY);
            Frames.Press();
            Frame();
        }

        public void MoveTo(int index)
        {
            Frames.MovePointer(index * CharWidth, MiddleY);
            Frame();
        }

        public void Release()
        {
            Frames.Release();
            Frame();
        }

        public void Click(int index, bool fresh = true)
        {
            Press(index, fresh);
            Release();
        }
    }
}
