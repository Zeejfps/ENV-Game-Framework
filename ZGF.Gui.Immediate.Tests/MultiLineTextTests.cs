using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MultiLineTextTests
{
    private static IEnumerable<string> Drawn(UiFrames frames) => frames.Canvas.Texts.Select(t => t.Inputs.Text);

    [Fact]
    public void Newlines_break_lines_and_the_element_is_as_tall_as_its_lines()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Text("first\nsecond line\nthird", id: ui.GetId("t"))).Frame();

        var rect = frames.RectOf("t");

        Assert.Equal(["first", "second line", "third"], Drawn(frames));
        Assert.Equal(3 * 16f, rect.Height);
        Assert.Equal(11 * 8f, rect.Width);
        var tops = frames.TextRects().Select(t => t.Rect.Y).ToArray();
        Assert.Equal([0f, 16f, 32f], tops);
    }

    [Fact]
    public void Wrapped_text_breaks_between_words_to_fit_its_width()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Column(new Box { Width = 100f }))
                ui.Text("the quick brown fox jumps", id: ui.GetId("t"), wrap: true);
        }).Frame();

        var rect = frames.RectOf("t");

        Assert.Equal(["the quick", "brown fox", "jumps"], Drawn(frames));
        Assert.Equal(100f, rect.Width);
        Assert.Equal(3 * 16f, rect.Height);
    }

    [Fact]
    public void Wrapped_text_held_to_fewer_lines_ends_its_last_in_an_ellipsis()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Column(new Box { Width = 100f }))
                ui.Text("the quick brown fox jumps", id: ui.GetId("t"), wrap: true, maxLines: 2);
        }).Frame();

        var rect = frames.RectOf("t");

        Assert.Equal(["the quick", "brown fox j…"], Drawn(frames));
        Assert.Equal(2 * 16f, rect.Height);
    }

    [Fact]
    public void Wrapped_text_within_its_lines_is_shown_whole()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Column(new Box { Width = 100f }))
                ui.Text("the quick brown fox jumps", wrap: true, maxLines: 3);
        }).Frame();

        Assert.Equal(["the quick", "brown fox", "jumps"], Drawn(frames));
    }

    [Fact]
    public void A_word_longer_than_the_width_breaks_between_its_characters()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Column(new Box { Width = 40f }))
                ui.Text("a enormousword b", wrap: true);
        }).Frame();

        Assert.Equal(["a", "enorm", "ouswo", "rd b"], Drawn(frames));
    }

    [Fact]
    public void A_path_wraps_after_its_separators()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Column(new Box { Width = 120f }))
                ui.Text(@"C:\Program Files\dotnet\shared\Microsoft.NETCore.App", wrap: true);
        }).Frame();

        Assert.Equal([@"C:\Program", @"Files\dotnet\", @"shared\", "Microsoft.", "NETCore.App"], Drawn(frames));
    }

    [Fact]
    public void A_long_tooltip_wraps_into_a_block()
    {
        var message = string.Join(' ', Enumerable.Repeat("word", 40));
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            var b = ui.Interact("b");
            ui.Leaf(b.Id, new Box { Width = 100f, Height = 30f });
            ui.Tooltip(b, message);
        }, width: 800f, height: 600f).Frame();
        frames.MovePointer(50f, 15f).Frame();
        frames.Wait(0.6).Frame(2);

        var lines = frames.Canvas.Texts.Where(t => t.Inputs.Text.StartsWith("word")).ToArray();
        Assert.True(lines.Length > 1);
        Assert.All(lines, l => Assert.True(l.Inputs.Text.Length * 8f <= 360f));
    }

    [Fact]
    public void A_painter_draws_and_measures_a_slice_of_a_string()
    {
        var frames = new UiFrames(new DefaultUiState());
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

    [Fact]
    public void An_ellipsis_never_cuts_inside_a_grapheme_cluster()
    {
        var shown = new UiFrames(new DefaultUiState()).Run(0f, ui => ui.Ellipsize("a👍🏽bcdefgh", 40f));

        Assert.Equal("a…", shown);
    }
}

public sealed class KeyStrokesTests
{
    [Fact]
    public void Keys_iterate_in_order_and_take_removes_them()
    {
        var frames = new UiFrames(new DefaultUiState()).Key(KeyboardKey.A).Key(KeyboardKey.Escape).Key(KeyboardKey.B);

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
