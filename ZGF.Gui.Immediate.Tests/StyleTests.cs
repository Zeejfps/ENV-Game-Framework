namespace ZGF.Gui.Immediate.Tests;

public sealed class StyleTests
{
    [Fact]
    public void Each_frame_starts_on_the_dark_style_until_the_app_picks_one()
    {
        var frames = new UiFrames();

        var first = frames.Run(0f, ui =>
        {
            var before = ui.Style;
            ui.UseStyle(UiStyle.Light);
            return (before, after: ui.Style);
        });
        var next = frames.Run(0f, ui => ui.Style);

        Assert.Same(UiStyle.Dark, first.before);
        Assert.Same(UiStyle.Light, first.after);
        Assert.Same(UiStyle.Dark, next);
    }

    [Fact]
    public void The_window_takes_the_last_style_set_while_earlier_text_keeps_its_own()
    {
        var frames = new UiFrames();

        frames.Run(0f, ui =>
        {
            ui.Text("early");
            ui.UseStyle(UiStyle.Light);
            ui.Text("late");
        });

        Assert.Equal(UiStyle.Dark.Text, frames.TextColor("early"));
        Assert.Equal(UiStyle.Light.Text, frames.TextColor("late"));
        Assert.Equal(UiStyle.Light.Background, frames.WindowBackground);
    }

    [Fact]
    public void A_provided_style_applies_only_inside_its_scope()
    {
        var frames = new UiFrames();

        var seen = frames.Run(0f, ui =>
        {
            UiStyle inside;
            using (ui.Provide(UiStyle.Light))
                inside = ui.Style;
            return (inside, after: ui.Style);
        });

        Assert.Same(UiStyle.Light, seen.inside);
        Assert.Same(UiStyle.Dark, seen.after);
    }

    [Fact]
    public void Text_takes_the_color_of_the_style_it_was_recorded_in()
    {
        var frames = new UiFrames();
        var red = UiStyle.Dark with { Text = 0xFFFF0000 };

        frames.Run(0f, ui =>
        {
            using (ui.Provide(red))
                ui.Text("tinted");
            ui.Text("plain");
        });

        Assert.Equal(0xFFFF0000u, frames.TextColor("tinted"));
        Assert.Equal(UiStyle.Dark.Text, frames.TextColor("plain"));
    }

    [Theory]
    [InlineData(0xFF000000u, 0xFFFFFFFFu, 0f, 0xFF000000u)]
    [InlineData(0xFF000000u, 0xFFFFFFFFu, 1f, 0xFFFFFFFFu)]
    [InlineData(0xFF000000u, 0xFFFFFFFFu, 0.5f, 0xFF808080u)]
    [InlineData(0x00204060u, 0xFF204060u, 0.5f, 0x80204060u)]
    public void Mix_blends_every_channel(uint a, uint b, float t, uint expected)
    {
        Assert.Equal(expected, UiColor.Mix(a, b, t));
    }
}
