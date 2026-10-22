namespace ZGF.Gui.Immediate.Tests;

public sealed class FontFamilyTests
{
    [Fact]
    public void Text_is_drawn_in_the_family_it_names()
    {
        var frames = new UiFrames(body: ui =>
        {
            ui.Text("mono", family: "Mono");
            ui.Text("plain");
        }).Frame();

        Assert.Equal("Mono", Family(frames, "mono"));
        Assert.Null(Family(frames, "plain"));
    }

    [Fact]
    public void Measurements_are_kept_apart_per_family()
    {
        var frames = new UiFrames(new FamilyMeasurer());

        var (plain, mono, again, lineHeight, monoLineHeight) = frames.Run(0f, ui => (
            ui.MeasureText("abcd"),
            ui.MeasureText("abcd", family: "Mono"),
            ui.MeasureText("abcd"),
            ui.LineHeight(),
            ui.LineHeight(family: "Mono")));

        Assert.Equal(32f, plain);
        Assert.Equal(40f, mono);
        Assert.Equal(32f, again);
        Assert.Equal(16f, lineHeight);
        Assert.Equal(20f, monoLineHeight);
        Assert.Equal(10f, frames.Run(0f, ui => ui.MeasureText("abcd", 1, family: "Mono")));
    }

    [Fact]
    public void Text_is_laid_out_with_its_familys_metrics()
    {
        var frames = new UiFrames(body: ui => ui.Text("abcd", family: "Mono", id: ui.GetId("t")), measurer: new FamilyMeasurer()).Frame();

        var rect = frames.RectOf("t");

        Assert.Equal(40f, rect.Width);
        Assert.Equal(20f, rect.Height);
    }

    [Fact]
    public void A_styles_family_is_the_default_inside_its_scope()
    {
        var mono = UiTheme.Dark with { FontFamily = "Mono" };

        var frames = new UiFrames(body: ui =>
        {
            using (ui.Provide(mono))
            {
                ui.Text("styled");
                ui.Text("override", family: "Icons");
            }
            ui.Text("outside");
        }).Frame();

        Assert.Equal("Mono", Family(frames, "styled"));
        Assert.Equal("Icons", Family(frames, "override"));
        Assert.Null(Family(frames, "outside"));
    }

    [Fact]
    public void Painted_text_takes_a_family()
    {
        var frames = new UiFrames(body: ui =>
        {
            using (ui.Box(new Box { Width = 100f, Height = 20f }))
                ui.Paint(static p => p.Text(p.Rect, "painted", 0xFFFFFFFF, family: "Mono"));
        }).Frame();

        Assert.Equal("Mono", Family(frames, "painted"));
    }

    private static string? Family(UiFrames frames, string text)
    {
        var theme = frames.Canvas.Texts.Single(t => t.Inputs.Text == text).Inputs.Style;
        return theme.FontFamily.IsSet ? theme.FontFamily.Value : null;
    }

    private sealed class FamilyMeasurer : ITextMeasurer
    {
        public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle theme) => text.Length * Advance(theme);

        public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle theme) =>
            Math.Clamp(prefixLength, 0, text.Length) * Advance(theme);

        public float MeasureTextLineHeight(TextStyle theme) => IsMono(theme) ? 20f : 16f;

        private static float Advance(TextStyle theme) => IsMono(theme) ? 10f : 8f;

        private static bool IsMono(TextStyle theme) => theme.FontFamily.IsSet && theme.FontFamily.Value == "Mono";
    }
}
