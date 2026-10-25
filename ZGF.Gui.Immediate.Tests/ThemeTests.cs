using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class ThemeTests
{
    public static TheoryData<string> Presets => new() { "Dark", "Light" };

    [Theory]
    [MemberData(nameof(Presets))]
    public void Every_preset_keeps_its_text_readable(string name)
    {
        var theme = Preset(name);

        Assert.InRange(Contrast(theme.Text, theme.Background), 4.5f, 21f);
        Assert.InRange(Contrast(theme.Text, theme.EditorBackground), 4.5f, 21f);
        Assert.InRange(Contrast(theme.TextMuted, theme.Background), 4.5f, 21f);
        Assert.InRange(Contrast(theme.TextMuted, theme.Surface), 4.5f, 21f);
        Assert.InRange(Contrast(theme.SelectionText, theme.Selection), 4.5f, 21f);
        Assert.InRange(Contrast(theme.Text, theme.TextSelection), 3.5f, 21f);
        Assert.InRange(Contrast(theme.OnAccent, theme.Accent), 4.5f, 21f);
        Assert.InRange(Contrast(theme.AccentText, theme.Background), 4.5f, 21f);
        Assert.Equal(!name.EndsWith("Light"), theme.IsDark);
    }

    [Theory]
    [MemberData(nameof(Presets))]
    public void Every_preset_shows_selected_text_on_a_selected_row(string name)
    {
        var theme = Preset(name);

        Assert.InRange(Contrast(theme.TextSelection, theme.Selection), 1.15f, 21f);
        Assert.InRange(Contrast(theme.TextSelection, theme.SelectionInactive), 1.15f, 21f);
        Assert.InRange(Contrast(theme.TextSelection, theme.Background), 1.15f, 21f);
    }

    private static UiTheme Preset(string name) => name == "Light" ? UiTheme.Light : UiTheme.Dark;

    private static float Contrast(uint a, uint b)
    {
        var (la, lb) = (UiColor.Luminance(a), UiColor.Luminance(b));
        return (Math.Max(la, lb) + 0.05f) / (Math.Min(la, lb) + 0.05f);
    }

    [Fact]
    public void The_theme_is_the_states_from_when_it_is_set_and_dark_before()
    {
        var seen = new UiFrames(new DefaultUiState()).Run(0f, ui =>
        {
            var before = ui.Theme;
            ui.State.Theme = UiTheme.Light;
            return (before, after: ui.Theme);
        });

        Assert.Same(UiTheme.Dark, seen.before);
        Assert.Same(UiTheme.Light, seen.after);
    }

    [Fact]
    public void Text_takes_the_color_of_the_theme_it_was_recorded_in()
    {
        var red = UiTheme.Dark with { Text = 0xFFFF0000 };

        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            ui.State.Theme = red;
            ui.Text("tinted");
            ui.State.Theme = UiTheme.Dark;
            ui.Text("plain");
        }).Frame();

        Assert.Equal(0xFFFF0000u, frames.TextColor("tinted"));
        Assert.Equal(UiTheme.Dark.Text, frames.TextColor("plain"));
    }

    [Fact]
    public void The_window_itself_draws_nothing_and_adds_no_padding()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Leaf(ui.GetId("a"), new Box { Width = 10f, Height = 10f })).Frame();

        var rect = frames.RectOf("a");

        Assert.Equal(0, frames.RectCount);
        Assert.Equal((0f, 0f), (rect.X, rect.Y));
    }

    [Fact]
    public void A_screen_fills_the_window_with_its_theme_and_pads_its_content()
    {
        var theme = UiTheme.Light with { ScreenPadding = 10f };
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Screen())
            {
                ui.Text("inside");
                ui.Leaf(ui.GetId("a"), new Box { Width = 10f, Height = 10f });
            }
        });
        frames.State.Theme = theme;
        frames.Frame();

        var rect = frames.RectOf("a");

        Assert.Equal(theme.Background, frames.FirstRect.Style.BackgroundColor);
        Assert.Equal((400f, 300f), (frames.FirstRect.Position.Width, frames.FirstRect.Position.Height));
        Assert.Equal(theme.Text, frames.TextColor("inside"));
        Assert.Equal(10f, rect.X);
    }

    [Fact]
    public void A_scroll_keeps_the_gutter_of_the_theme_it_was_recorded_in()
    {
        var frames = new UiFrames(new DefaultUiState());
        frames.State.Theme = UiTheme.Dark with { ScrollBarWidth = 20f };

        float RowWidth() => frames.Run(1f / 60f, ui =>
        {
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

    [Theory]
    [InlineData("#1B1E24", 0xFF1B1E24u)]
    [InlineData("#1b1e24", 0xFF1B1E24u)]
    [InlineData("#44475A80", 0x8044475Au)]
    [InlineData("#00000066", 0x66000000u)]
    public void A_css_color_reads_as_argb(string text, uint expected)
    {
        Assert.True(UiColor.TryParse(text, out var color));
        Assert.Equal(expected, color);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1B1E24")]
    [InlineData("#1B1E2")]
    [InlineData("#1B1E24F")]
    [InlineData("#GGGGGG")]
    [InlineData("#+1B1E2")]
    [InlineData("#1B1E24FF0")]
    public void Anything_else_is_no_color(string text)
    {
        Assert.False(UiColor.TryParse(text, out _));
    }

    [Fact]
    public void A_theme_file_must_set_the_base_roles_and_may_set_the_derived_ones()
    {
        var required = UiTheme.ColorKeys
            .Where(k => k.IsRequired)
            .Select(k => k.Key);

        Assert.Equal(["background", "surface", "control", "field", "border", "text", "textMuted", "accent", "onAccent"], required);
        Assert.False(KeyOf("controlHovered").IsRequired);
        Assert.False(KeyOf("popupShadowColor").IsRequired);
    }

    [Fact]
    public void Each_key_tells_what_its_color_is_for_from_the_summary_of_its_property()
    {
        Assert.Equal("The workspace: the window, trees and lists.", KeyOf("background").Description);
        Assert.Equal("Docked chrome: menu bar, main toolbar, tab strip, status bar. A pane's own body and toolbar use paneBackground.", KeyOf("surface").Description);
        Assert.Equal("Text on selection and selectionInactive.", KeyOf("selectionText").Description);
        Assert.Equal("The shadow under menus, context menus and tooltips.", KeyOf("popupShadowColor").Description);
        Assert.Equal("Derived from other colors, or a default, when unset.", KeyOf("controlHovered").Description);
        Assert.Equal("", KeyOf("text").Description);
    }

    private static ThemeColorKey KeyOf(string key) => UiTheme.ColorKeys.Single(k => k.Key == key);

    [Fact]
    public void A_theme_lists_its_colors_by_key_as_its_getters_give_them()
    {
        var theme = new UiTheme { Control = 0xFF303030, Text = 0xFFF0F0F0, PopupShadow = new UiShadow(0f, 1f, 2f, 0f, 0x40000000) };

        var colors = theme.ToColors();

        Assert.Equal(UiTheme.ColorKeys.Select(k => k.Key), colors.Keys);
        Assert.Equal(0xFF303030u, colors["control"]);
        Assert.Equal(UiColor.Mix(0xFF303030, 0xFFF0F0F0, 0.06f), colors["controlHovered"]);
        Assert.Equal(0x40000000u, colors["popupShadowColor"]);
    }

    [Theory]
    [InlineData(0xFF1B1E24u, "#1B1E24")]
    [InlineData(0x8044475Au, "#44475A80")]
    [InlineData(0x00FFFFFFu, "#FFFFFF00")]
    public void A_color_is_written_as_css_reads_it_back(uint color, string hex)
    {
        Assert.Equal(hex, UiColor.ToHex(color));
        Assert.True(UiColor.TryParse(hex, out var read));
        Assert.Equal(color, read);
    }

    [Fact]
    public void A_file_asked_to_be_opened_is_handed_to_the_host_once_the_frame_is_drawn()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.OpenFile("theme.json"));

        frames.Frame();

        Assert.Equal(["theme.json"], frames.OpenedFiles);
    }

    [Fact]
    public void A_theme_built_from_colors_derives_what_they_leave_out()
    {
        var colors = new Dictionary<string, uint>
        {
            ["background"] = 0xFF101010,
            ["control"] = 0xFF303030,
            ["text"] = 0xFFF0F0F0,
            ["dialogShadowColor"] = 0x40000000,
        };

        var theme = UiTheme.FromColors(colors);

        Assert.Equal(0xFF101010u, theme.Background);
        Assert.Equal(UiColor.Mix(0xFF303030, 0xFFF0F0F0, 0.06f), theme.ControlHovered);
        Assert.Equal(new UiTheme().DialogShadow with { Color = 0x40000000 }, theme.DialogShadow);
        Assert.Equal(new UiTheme().PopupShadow, theme.PopupShadow);
    }
}
