using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class ThemeTests
{
    public static TheoryData<string> Presets => new() { "Dark", "Light", "GitHubDark", "OneDarkPro", "Dracula", "Monokai", "CatppuccinMocha" };

    [Theory]
    [MemberData(nameof(Presets))]
    public void Every_preset_keeps_its_text_readable(string name)
    {
        var theme = (UiTheme)typeof(UiTheme).GetProperty(name)!.GetValue(null)!;

        Assert.InRange(Contrast(theme.Text, theme.Background), 4.5f, 21f);
        Assert.InRange(Contrast(theme.Text, theme.EditorBackground), 4.5f, 21f);
        Assert.InRange(Contrast(theme.TextMuted, theme.Background), 4.5f, 21f);
        Assert.InRange(Contrast(theme.TextMuted, theme.Surface), 4.5f, 21f);
        Assert.InRange(Contrast(theme.SelectionText, theme.Selection), 4.5f, 21f);
        Assert.InRange(Contrast(theme.OnAccent, theme.Accent), 4.5f, 21f);
        Assert.InRange(Contrast(theme.AccentText, theme.Background), 4.5f, 21f);
        Assert.Equal(name != "Light", theme.IsDark);
    }

    private static float Contrast(uint a, uint b)
    {
        var (la, lb) = (UiColor.Luminance(a), UiColor.Luminance(b));
        return (Math.Max(la, lb) + 0.05f) / (Math.Min(la, lb) + 0.05f);
    }

    [Fact]
    public void A_provided_theme_applies_only_inside_its_scope_and_dark_outside_any()
    {
        var frames = new UiFrames();

        var seen = frames.Run(0f, ui =>
        {
            var before = ui.Theme;
            UiTheme inside;
            using (ui.Provide(UiTheme.Light))
                inside = ui.Theme;
            return (before, inside, after: ui.Theme);
        });

        Assert.Same(UiTheme.Dark, seen.before);
        Assert.Same(UiTheme.Light, seen.inside);
        Assert.Same(UiTheme.Dark, seen.after);
    }

    [Fact]
    public void Text_takes_the_color_of_the_theme_it_was_recorded_in()
    {
        var frames = new UiFrames();
        var red = UiTheme.Dark with { Text = 0xFFFF0000 };

        frames.Run(0f, ui =>
        {
            using (ui.Provide(red))
                ui.Text("tinted");
            ui.Text("plain");
        });

        Assert.Equal(0xFFFF0000u, frames.TextColor("tinted"));
        Assert.Equal(UiTheme.Dark.Text, frames.TextColor("plain"));
    }

    [Fact]
    public void The_window_itself_draws_nothing_and_adds_no_padding()
    {
        var frames = new UiFrames();

        UiRect Frame() => frames.Run(0f, ui =>
        {
            ui.Leaf(ui.GetId("a"), new Box { Width = 10f, Height = 10f });
            return ui.TryGetRect(ui.GetId("a"), out var r) ? r : default;
        });

        Frame();
        var rect = Frame();

        Assert.Equal(0, frames.RectCount);
        Assert.Equal((0f, 0f), (rect.X, rect.Y));
    }

    [Fact]
    public void A_screen_fills_the_window_with_its_theme_and_pads_its_content()
    {
        var frames = new UiFrames();
        var theme = UiTheme.Light with { ScreenPadding = 10f };

        UiRect Frame() => frames.Run(0f, ui =>
        {
            using (ui.Screen(theme))
            {
                ui.Text("inside");
                ui.Leaf(ui.GetId("a"), new Box { Width = 10f, Height = 10f });
            }
            return ui.TryGetRect(ui.GetId("a"), out var r) ? r : default;
        });

        Frame();
        var rect = Frame();

        Assert.Equal(theme.Background, frames.FirstRect.Style.BackgroundColor);
        Assert.Equal((400f, 300f), (frames.FirstRect.Position.Width, frames.FirstRect.Position.Height));
        Assert.Equal(theme.Text, frames.TextColor("inside"));
        Assert.Equal(10f, rect.X);
    }

    [Fact]
    public void A_scroll_keeps_the_gutter_of_the_theme_it_was_recorded_in()
    {
        var frames = new UiFrames();
        var wide = UiTheme.Dark with { ScrollBarWidth = 20f };

        float RowWidth() => frames.Run(1f / 60f, ui =>
        {
            using (ui.Provide(wide))
            using (ui.Scroll("wide"))
            {
                for (var i = 0; i < 20; i++)
                    ui.Leaf(ui.GetId(i), new Box { Width = Sizing.Grow(), Height = 40f });
            }
            return ui.TryGetRect(ui.GetId(0), out var rect) ? rect.Width : float.NaN;
        });

        RowWidth();
        RowWidth();

        Assert.Equal(400f - (20f + 2 * UiTheme.Dark.ScrollBarInset), RowWidth(), 0.01f);
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
