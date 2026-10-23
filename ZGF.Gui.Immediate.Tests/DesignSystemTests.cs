using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class EllipsisTests
{
    private readonly UiFrames _frames = new();

    private static string[] Drawn(UiFrames frames) => frames.Canvas.Texts.Select(t => t.Inputs.Text).ToArray();

    [Fact]
    public void Text_wider_than_its_box_ends_in_an_ellipsis_on_the_first_frame()
    {
        _frames.Run(0f, ui => ui.Text("abcdefghijklmnop", new Box { Width = 80f }, overflow: TextOverflow.Ellipsis));

        Assert.Contains("abcdefghi…", Drawn(_frames));
    }

    [Fact]
    public void A_middle_ellipsis_keeps_both_ends()
    {
        _frames.Run(0f, ui => ui.Text("abcdefghijklmnop", new Box { Width = 80f }, overflow: TextOverflow.EllipsisMiddle));

        Assert.Contains("abcde…mnop", Drawn(_frames));
    }

    [Fact]
    public void Text_that_fits_is_drawn_whole()
    {
        _frames.Run(0f, ui => ui.Text("short", new Box { Width = 80f }, overflow: TextOverflow.Ellipsis));

        Assert.Contains("short", Drawn(_frames));
    }

    [Fact]
    public void Text_with_an_overflow_shrinks_to_the_room_its_row_has_left()
    {
        var frames = new UiFrames(body: ui =>
        {
            using (ui.Row(new Box { Width = 100f, Gap = 0f }))
            {
                ui.Leaf(new Box { Width = 20f, Height = 10f });
                ui.Text("abcdefghijklmnop", id: ui.GetId("label"), overflow: TextOverflow.Ellipsis);
            }
        }).Frame();

        Assert.Equal(80f, frames.RectOf("label").Width);
        Assert.Contains("abcdefghi…", Drawn(frames));
    }

    [Fact]
    public void Text_with_an_overflow_keeps_its_own_width_when_there_is_room()
    {
        var frames = new UiFrames(body: ui =>
        {
            using (ui.Row(new Box { Width = 300f }))
                ui.Text("abc", id: ui.GetId("label"), overflow: TextOverflow.Ellipsis);
        }).Frame();

        Assert.Equal(24f, frames.RectOf("label").Width);
    }

    [Fact]
    public void An_unchanged_ellipsized_text_allocates_no_more_than_the_same_text_unshortened()
    {
        static void Shortened(Ui ui) => ui.Text("abcdefghijklmnop", new Box { Width = 80f }, overflow: TextOverflow.EllipsisMiddle);
        static void Plain(Ui ui) => ui.Text("abcde…mnop", new Box { Width = 80f });

        Assert.Equal(AllocatedPerRun(Plain), AllocatedPerRun(Shortened));
    }

    private long AllocatedPerRun(Action<Ui> body)
    {
        for (var i = 0; i < 4; i++)
            _frames.Run(0f, body);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
            _frames.Run(0f, body);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void Ellipsize_answers_painters_with_the_same_text()
    {
        var shown = _frames.Run(0f, ui => ui.Ellipsize("abcdefghijklmnop", 80f));

        Assert.Equal("abcdefghi…", shown);
    }
}

public sealed class BoxOverrideTests
{
    [Fact]
    public void A_panel_can_be_asked_for_square_corners_and_no_border()
    {
        var frames = new UiFrames(body: ui =>
        {
            using (ui.Panel(new Box { Radius = 0f, BorderWidth = 0f })) { }
        }).Frame();

        var panel = frames.FirstRect.Style;
        Assert.Equal(0f, panel.BorderRadius.TopLeft.Value);
        Assert.Equal(0f, panel.BorderSize.Top.Value);
    }

    [Fact]
    public void An_unset_panel_takes_the_themes_border_and_panel_radius()
    {
        var frames = new UiFrames(body: ui =>
        {
            using (ui.Panel()) { }
        }).Frame();

        var panel = frames.FirstRect.Style;
        Assert.Equal(UiTheme.Dark.PanelRadius, panel.BorderRadius.TopLeft.Value);
        Assert.Equal(UiTheme.Dark.BorderWidth, panel.BorderSize.Top.Value);
    }
}

public sealed class ThemeRoleTests
{
    [Fact]
    public void Compact_density_changes_geometry_and_keeps_the_palette()
    {
        var compact = UiTheme.Dark.WithDensity(UiDensity.Compact);

        Assert.Equal(28f, compact.ControlHeight);
        Assert.Equal(24f, compact.RowHeight);
        Assert.Equal(32f, compact.TabHeight);
        Assert.Equal(13f, compact.FontSize);
        Assert.Equal(UiTheme.Dark.Selection, compact.Selection);
        Assert.Equal(UiTheme.Dark.Background, compact.Background);
        Assert.Equal(UiTheme.Dark, compact.WithDensity(UiDensity.Comfortable));
    }

    [Fact]
    public void Unset_state_roles_are_derived_from_the_base_roles()
    {
        var theme = new UiTheme { Background = 0xFF101010, Surface = 0xFF202020, Control = 0xFF303030, Text = 0xFFFFFFFF, Accent = 0xFF0000FF };

        Assert.Equal(theme.Surface, theme.SurfaceRaised);
        Assert.Equal(theme.Accent, theme.FocusRing);
        Assert.Equal(theme.Text, theme.SelectionText);
        Assert.NotEqual(theme.Control, theme.ControlHovered);
        Assert.NotEqual(0u, theme.Selection);
    }

    [Fact]
    public void Set_state_roles_win_over_derivation_and_feed_the_hover_helpers()
    {
        var theme = UiTheme.Dark with { ControlHovered = 0xFF123456, Selection = 0xFF654321 };

        Assert.Equal(0xFF123456u, theme.Hovered(theme.Control));
        Assert.Equal(0xFF654321u, theme.Selection);
    }

    [Fact]
    public void A_selected_tab_its_toolbar_and_table_header_share_the_pane_surface_apart_from_the_strip()
    {
        var theme = UiTheme.Dark with { PaneBackground = 0xFF0A0B0C };

        Assert.Equal(
            [0xFF0A0B0Cu, 0xFF0A0B0Cu, 0xFF0A0B0Cu],
            [theme.TabActiveBackground, theme.PaneToolbarBackground, theme.TableHeaderBackground]);
        Assert.Equal(UiTheme.Dark.EditorBackground, UiTheme.Dark.PaneBackground);
        Assert.Equal(theme.Surface, theme.TabStripBackground);
        Assert.NotEqual(theme.TabStripBackground, theme.TabActiveBackground);
    }

    [Fact]
    public void A_pane_toolbar_is_painted_with_the_pane_surface()
    {
        var theme = UiTheme.Dark with { PaneToolbarBackground = 0xFF0A0B0C };

        var frames = new UiFrames(body: ui =>
        {
            using (ui.PaneToolbar())
                ui.Text("tools");
        });
        frames.State.Theme = theme;
        frames.Frame();

        var bar = Assert.Single(frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == 0xFF0A0B0Cu);
        Assert.Equal(theme.ToolbarHeight, bar.Inputs.Position.Height);
    }
}

public sealed class FocusTests
{
    private readonly UiFrames _frames;
    private readonly List<string> _clicked = new();
    private bool _enabled = true;

    public FocusTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        if (ui.Button("One")) _clicked.Add("One");
        if (ui.Button("Two", enabled: _enabled)) _clicked.Add("Two");
        if (ui.IconButton("three", "+", tooltip: "Three")) _clicked.Add("Three");
    }

    private void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => _frames.Key(key, modifiers).Frame();

    private bool Focused(string label) => _frames.Frame(ui => ui.HasFocus(ui.GetId(label)));

    [Fact]
    public void Tab_walks_the_controls_in_recording_order_and_wraps()
    {
        _frames.Frame();

        Key(KeyboardKey.Tab);
        Assert.True(Focused("One"));
        Key(KeyboardKey.Tab);
        Assert.True(Focused("Two"));
        Key(KeyboardKey.Tab);
        Key(KeyboardKey.Tab);
        Assert.True(Focused("One"));
        Key(KeyboardKey.Tab, KeyModifiers.Shift);
        Assert.True(Focused("three"));
    }

    [Fact]
    public void Enter_and_space_activate_the_focused_button()
    {
        _frames.Frame();
        Key(KeyboardKey.Tab);

        Key(KeyboardKey.Enter);
        Key(KeyboardKey.Tab);
        Key(KeyboardKey.Space);

        Assert.Equal(["One", "Two"], _clicked);
    }

    [Fact]
    public void A_disabled_button_is_skipped()
    {
        _enabled = false;
        _frames.Frame();

        Key(KeyboardKey.Tab);
        Key(KeyboardKey.Tab);

        Assert.True(Focused("three"));
    }

    [Fact]
    public void Focus_moved_by_tab_is_visible_and_a_press_hides_it()
    {
        _frames.Frame();
        Key(KeyboardKey.Tab);

        Assert.True(_frames.Frame(ui => ui.FocusVisible));
        Assert.Contains(_frames.Canvas.Rects, r => r.Inputs.Style.BorderColor.Top.Value == UiTheme.Dark.FocusRing);

        _frames.Press().Frame();
        _frames.Release().Frame();

        Assert.DoesNotContain(_frames.Canvas.Rects, r => r.Inputs.Style.BorderColor.Top.Value == UiTheme.Dark.FocusRing);
    }

    [Fact]
    public void A_text_field_takes_text_when_tab_reaches_it()
    {
        var text = "";
        _frames.Body = ui =>
        {
            ui.Button("Before");
            ui.TextField("field", ref text);
        };

        _frames.Frame();
        Key(KeyboardKey.Tab);
        Key(KeyboardKey.Tab);

        Assert.True(_frames.WantsTextInput);
    }
}

public sealed class ButtonStyleTests
{
    [Fact]
    public void A_primary_button_is_filled_with_the_accent()
    {
        var frames = new UiFrames(body: ui => ui.Button("Open", variant: ButtonVariant.Primary)).Frame();

        Assert.Equal(UiTheme.Dark.Accent, frames.FirstRect.Style.BackgroundColor);
        Assert.Equal(UiTheme.Dark.OnAccent, frames.TextColor("Open"));
    }

    [Fact]
    public void A_ghost_button_has_no_fill_until_hovered()
    {
        var frames = new UiFrames(body: ui => ui.Button("Back", variant: ButtonVariant.Ghost)).Frame();

        Assert.Equal(0, frames.RectCount);
    }

    [Fact]
    public void A_disabled_button_uses_disabled_text_and_no_hand_cursor()
    {
        var frames = new UiFrames(body: ui => ui.Button("Save", enabled: false)).Frame();
        frames.MovePointer(10f, 10f).Frame(2);

        Assert.Equal(UiTheme.Dark.TextDisabled, frames.TextColor("Save"));
        Assert.Equal(MouseCursor.Default, frames.Cursor);
    }

    [Fact]
    public void An_icon_button_is_a_square_of_the_icon_button_size_with_a_tooltip()
    {
        var frames = new UiFrames(body: ui => ui.IconButton("back", "<", tooltip: "Back")).Frame();

        var rect = frames.RectOf("back");
        Assert.Equal(UiTheme.Dark.IconButtonSize, rect.Width);
        Assert.Equal(UiTheme.Dark.IconButtonSize, rect.Height);

        frames.MovePointer(rect.X + 4f, rect.Y + 4f).Frame();
        frames.Wait(1.0).Frame();
        Assert.True(frames.Drew("Back"));
    }
}

public sealed class TabStyleTests
{
    private readonly UiFrames _frames = new(width: 1200f, body: Body);

    private static void Body(Ui ui)
    {
        using var strip = ui.TabStrip("tabs");
        strip.Tab(new string('x', 100), selected: true, tooltip: "Everything");
        strip.Tab("b", selected: false);
    }

    [Fact]
    public void A_long_tab_is_capped_at_the_max_width_and_ellipsized()
    {
        _frames.Frame();
        var rect = _frames.RectOf(new string('x', 100));

        Assert.Equal(UiTheme.Dark.TabMaxWidth, rect.Width);
        Assert.Contains(_frames.Canvas.Texts, t => t.Inputs.Text.EndsWith('…'));
    }

    [Fact]
    public void A_short_tab_is_at_least_the_min_width()
    {
        _frames.Frame();

        Assert.Equal(UiTheme.Dark.TabMinWidth, _frames.RectOf("b").Width);
    }

    [Fact]
    public void The_selected_tab_is_filled_like_the_pane_below_it()
    {
        _frames.Frame();

        Assert.Contains(_frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == UiTheme.Dark.PaneBackground);
    }
}

public sealed class PopupElevationTests
{
    [Fact]
    public void A_menus_window_grows_by_its_shadow_and_only_its_rect_takes_the_pointer()
    {
        var host = new TestPopupHost();
        var frames = new UiFrames(popups: host);
        void Body(Ui ui)
        {
            ui.OpenContextMenu("menu", new ZGF.Geometry.PointF(100f, 100f));
            using var menu = ui.ContextMenu("menu");
            if (menu.IsOpen) ui.MenuItem("Copy");
        }

        frames.Run(0f, Body);
        var id = frames.Run(0f, ui =>
        {
            using (var menu = ui.ContextMenu("menu"))
                if (menu.IsOpen) ui.MenuItem("Copy");
            return ui.GetId("menu");
        });

        var rect = host.Shown[id];
        var surface = host.Surfaces[id];
        var reach = UiTheme.Dark.PopupShadow.Extent;
        Assert.Equal(rect.X - reach.Left, surface.X);
        Assert.Equal(rect.Bottom + reach.Bottom, surface.Bottom);
        Assert.Contains(host.CanvasOf(id).Rects, r => r.Inputs.Style.BorderRadius.TopLeft.Value == UiTheme.Dark.PopupRadius);
    }
}

public sealed class ShrinkPriorityTests
{
    [Fact]
    public void A_lower_priority_text_gives_up_its_room_before_a_higher_one_shrinks()
    {
        var frames = new UiFrames(body: ui =>
        {
            using (ui.Row(new Box { Width = 100f, Gap = 0f }))
            {
                ui.Text("primary12", new Box { Width = Sizing.Fit() with { Priority = 1 } }, id: ui.GetId("primary"), overflow: TextOverflow.Ellipsis);
                ui.Text("secondary", id: ui.GetId("secondary"), overflow: TextOverflow.Ellipsis);
            }
        }).Frame();

        Assert.Equal(72f, frames.RectOf("primary").Width);
        Assert.Equal(28f, frames.RectOf("secondary").Width);
        Assert.True(frames.Drew("primary12"));
    }

    [Fact]
    public void Equal_priorities_share_the_shortfall()
    {
        var frames = new UiFrames(body: ui =>
        {
            using (ui.Row(new Box { Width = 100f, Gap = 0f }))
            {
                ui.Text("aaaaaaaaaa", id: ui.GetId("a"), overflow: TextOverflow.Ellipsis);
                ui.Text("bbbbbbbbbb", id: ui.GetId("b"), overflow: TextOverflow.Ellipsis);
            }
        }).Frame();

        Assert.Equal(50f, frames.RectOf("a").Width);
        Assert.Equal(50f, frames.RectOf("b").Width);
    }
}

public sealed class TextFieldSlotTests
{
    private readonly UiFrames _frames = new();
    private string _text = "hello";

    private void Body(Ui ui, float width, string? hint) =>
        ui.TextField("field", ref _text, width: Sizing.Fixed(width), leadingIcon: "Q", hint: hint);

    [Fact]
    public void A_click_places_the_caret_after_the_leading_icon()
    {
        _frames.Body = ui => Body(ui, 300f, null);
        _frames.Frame();
        var rect = _frames.RectOf("field");
        var textLeft = rect.X + UiTheme.Dark.FieldPaddingX + UiTheme.Dark.IconSize + UiTheme.Dark.GapTight;

        _frames.Click(textLeft + 2 * 8f + 1f, rect.Y + rect.Height / 2);
        _frames.Type("X").Frame();

        Assert.Equal("heXllo", _text);
        Assert.True(_frames.Drew("Q"));
    }

    [Fact]
    public void The_hint_shows_while_there_is_room_and_hides_when_narrow()
    {
        void Wide(Ui ui) => Body(ui, 400f, "Ctrl+K");
        void Narrow(Ui ui) => Body(ui, 120f, "Ctrl+K");

        _frames.Run(0f, Wide);
        _frames.Run(0f, Wide);
        Assert.True(_frames.Drew("Ctrl+K"));

        _frames.Run(0f, Narrow);
        _frames.Run(0f, Narrow);
        Assert.False(_frames.Drew("Ctrl+K"));
    }
}

public sealed class TreeRowSlotTests
{
    private sealed class Flat(TreeRow row) : ITreeModel<string>
    {
        public bool HasChildren(string node) => false;
        public IReadOnlyList<string> GetChildren(string node) => [];
        public TreeRow Row(string node) => row;
    }

    [Fact]
    public void The_secondary_text_ellipsizes_before_the_label_and_the_overlay_is_drawn()
    {
        var model = new Flat(new TreeRow("System.Runtime", new UiIcon("A", Overlay: "L"), Secondary: "C:/dotnet/shared/System.Runtime.dll", Tooltip: "full"));
        var state = new TreeViewState<string>(StringComparer.Ordinal);
        string[] roots = ["root"];

        var frames = new UiFrames(body: ui => ui.TreeView("tree", roots, model, state), width: 200f).Frame(2);

        Assert.True(frames.Drew("System.Runtime"));
        Assert.True(frames.Drew("L"));
        Assert.Contains(frames.Canvas.Texts, t => t.Inputs.Text.StartsWith("C:/") && t.Inputs.Text.EndsWith('\u2026'));
    }
}
