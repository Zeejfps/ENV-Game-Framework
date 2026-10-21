using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MultiLineTextTests
{
    private static IEnumerable<string> Drawn(UiFrames frames) => frames.Canvas.Texts.Select(t => t.Inputs.Text);

    [Fact]
    public void Newlines_break_lines_and_the_element_is_as_tall_as_its_lines()
    {
        var frames = new UiFrames();
        void Body(Ui ui) => ui.Text("first\nsecond line\nthird", id: ui.GetId("t"));
        frames.Run(0f, Body);

        var rect = frames.RectOf(Body, ui => ui.GetId("t"));

        Assert.Equal(["first", "second line", "third"], Drawn(frames));
        Assert.Equal(3 * 16f, rect.Height);
        Assert.Equal(11 * 8f, rect.Width);
        var tops = frames.Canvas.Texts.Select(t => 300f - t.Inputs.Position.Top).ToArray();
        Assert.Equal([0f, 16f, 32f], tops);
    }

    [Fact]
    public void Wrapped_text_breaks_between_words_to_fit_its_width()
    {
        var frames = new UiFrames();
        void Body(Ui ui)
        {
            using (ui.Column(new Box { Width = 100f }))
                ui.Text("the quick brown fox jumps", id: ui.GetId("t"), wrap: true);
        }
        frames.Run(0f, Body);

        var rect = frames.RectOf(Body, ui => ui.GetId("t"));

        Assert.Equal(["the quick", "brown fox", "jumps"], Drawn(frames));
        Assert.Equal(100f, rect.Width);
        Assert.Equal(3 * 16f, rect.Height);
    }

    [Fact]
    public void A_word_longer_than_the_width_gets_a_line_of_its_own()
    {
        var frames = new UiFrames();
        frames.Run(0f, ui =>
        {
            using (ui.Column(new Box { Width = 40f }))
                ui.Text("a enormousword b", wrap: true);
        });

        Assert.Equal(["a", "enormousword", "b"], Drawn(frames));
    }

    [Fact]
    public void A_long_tooltip_wraps_into_a_block()
    {
        var frames = new UiFrames(width: 800f, height: 600f);
        var message = string.Join(' ', Enumerable.Repeat("word", 40));
        void Body(Ui ui)
        {
            var b = ui.Interact("b");
            ui.Leaf(b.Id, new Box { Width = 100f, Height = 30f });
            ui.Tooltip(b, message);
        }
        frames.Run(0f, Body);
        frames.MovePointer(50f, 15f);
        frames.Run(0f, Body);
        frames.Wait(0.6);
        frames.Run(0f, Body);
        frames.Run(0f, Body);

        var lines = frames.Canvas.Texts.Where(t => t.Inputs.Text.StartsWith("word")).ToArray();
        Assert.True(lines.Length > 1);
        Assert.All(lines, l => Assert.True(l.Inputs.Text.Length * 8f <= 360f));
    }

    [Fact]
    public void A_painter_draws_and_measures_a_slice_of_a_string()
    {
        var frames = new UiFrames();
        const string line = "public static void Main";

        var width = frames.Run(0f, ui =>
        {
            using (ui.Box(new Box { Width = 200f, Height = 20f }))
                ui.Paint(static p => p.Text(p.Rect, line, 7, 6, 0xFFFFFFFF));
            return ui.MeasureText(line.AsSpan(7, 6));
        });

        Assert.Equal(48f, width);
        Assert.Equal(["static"], Drawn(frames));
    }
}

public sealed class KeyStrokesTests
{
    [Fact]
    public void Keys_iterate_in_order_and_take_removes_them()
    {
        var frames = new UiFrames();
        frames.Key(KeyboardKey.A);
        frames.Key(KeyboardKey.Escape);
        frames.Key(KeyboardKey.B);

        var (seen, taken, after) = frames.Run(0f, ui =>
        {
            var keys = new List<KeyboardKey>();
            foreach (var stroke in ui.Input.Keys)
                keys.Add(stroke.Key);
            var took = ui.Input.TakeKey(KeyboardKey.Escape);
            return (keys, took, ui.Input.Keys.Count);
        });

        Assert.Equal([KeyboardKey.A, KeyboardKey.Escape, KeyboardKey.B], seen);
        Assert.True(taken);
        Assert.Equal(2, after);
    }
}
