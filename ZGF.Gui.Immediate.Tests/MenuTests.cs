using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MenuTests
{
    private readonly UiFrames _frames;
    private readonly List<string> _picks = new();
    private bool _dark;
    private int _underClicks;

    public MenuTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using (ui.MenuBar("bar"))
        {
            using (var file = ui.Menu("File"))
            {
                if (file.IsOpen)
                {
                    if (ui.MenuItem("Open", "Ctrl+O")) _picks.Add("Open");
                    ui.MenuSeparator();
                    if (ui.MenuItem("Save", enabled: false)) _picks.Add("Save");
                }
            }
            using (var view = ui.Menu("View"))
            {
                if (view.IsOpen)
                    ui.MenuItem("Dark", ref _dark);
            }
        }

        var under = ui.Interact("under");
        if (under.Clicked) _underClicks++;
        ui.Leaf(under.Id, new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
    }

    private void OpenFile() => _frames.Frame().ClickOn("File").Frame();

    private bool Open(string item) => _frames.Frame().Drew(item);

    [Fact]
    public void Clicking_a_title_opens_its_dropdown_with_shortcuts()
    {
        OpenFile();

        Assert.True(Open("Open"));
        Assert.True(_frames.Drew("Ctrl+O"));
        Assert.False(_frames.Drew("Dark"));
    }

    [Fact]
    public void Picking_an_item_reports_it_closes_the_menu_and_does_not_click_through()
    {
        OpenFile();

        _frames.ClickOn("Open");

        Assert.Equal(["Open"], _picks);
        Assert.False(Open("Open"));
        Assert.Equal(0, _underClicks);
    }

    [Fact]
    public void A_disabled_item_cannot_be_picked()
    {
        OpenFile();

        _frames.ClickOn("Save");

        Assert.Empty(_picks);
        Assert.True(Open("Open"));
    }

    [Fact]
    public void Hovering_another_title_switches_the_open_menu()
    {
        OpenFile();
        var view = _frames.RectOf("View");

        _frames.MovePointer(view.X + 5f, view.Y + 5f).Frame(2);

        Assert.True(Open("Dark"));
        Assert.False(_frames.Drew("Open"));
    }

    [Fact]
    public void Escape_closes_the_menu()
    {
        OpenFile();

        _frames.Key(KeyboardKey.Escape).Frame();

        Assert.False(Open("Open"));
    }

    [Fact]
    public void A_press_outside_closes_the_menu()
    {
        OpenFile();

        _frames.Click(350f, 250f);

        Assert.False(Open("Open"));
        Assert.Equal(1, _underClicks);
    }

    [Fact]
    public void A_dismissal_from_the_platform_closes_the_menu()
    {
        OpenFile();

        _frames.DismissPopups().Frame();

        Assert.False(Open("Open"));
    }

    [Fact]
    public void A_checkable_item_flips_its_value()
    {
        _frames.Frame().ClickOn("View").Frame();

        _frames.ClickOn("Dark");

        Assert.True(_dark);
    }
}

public sealed class ContextMenuTests
{
    private readonly UiFrames _frames;
    private readonly List<string> _picks = new();

    public ContextMenuTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        var target = ui.Interact("target");
        if (target.ClickedWith(PointerButton.Right))
            ui.OpenContextMenu("ctx");
        ui.Leaf(target.Id, new Box { Width = 200f, Height = 100f });

        using var menu = ui.ContextMenu("ctx");
        if (menu.IsOpen && ui.MenuItem("Copy")) _picks.Add("Copy");
    }

    [Fact]
    public void A_right_click_opens_the_menu_at_the_pointer_and_a_pick_closes_it()
    {
        _frames.Frame().Click(50f, 40f, PointerButton.Right).Frame();
        var rect = _frames.RectOf("ctx");
        Assert.Equal((50f, 40f), (rect.X, rect.Y));

        _frames.ClickOn("Copy").Frame();

        Assert.Equal(["Copy"], _picks);
        Assert.False(_frames.Drew("Copy"));
    }

    [Fact]
    public void A_press_outside_closes_the_menu()
    {
        _frames.Frame().Click(50f, 40f, PointerButton.Right).Frame();
        Assert.True(_frames.Drew("Copy"));

        _frames.Click(350f, 250f);

        Assert.False(_frames.Drew("Copy"));
        Assert.Empty(_picks);
    }
}

public sealed class TooltipTests
{
    private readonly UiFrames _frames = new(body: Body);

    private static void Body(Ui ui)
    {
        using var screen = ui.Screen();
        var b = ui.Interact("b");
        ui.Leaf(b.Id, new Box { Width = 100f, Height = 30f });
        ui.Tooltip(b, "Tip");
    }

    [Fact]
    public void A_tooltip_waits_for_the_hover_delay_then_shows_under_its_target()
    {
        _frames.Frame().MovePointer(50f, 30f).Frame(2);

        Assert.False(_frames.Drew("Tip"));
        Assert.Equal(_frames.RealTime + 0.5, _frames.WakeAt, 3);

        _frames.Wait(0.6).Frame();

        Assert.True(_frames.Drew("Tip"));
        Assert.True(_frames.TextRect("Tip").Y >= 46f + 6f);
    }

    [Fact]
    public void Leaving_or_pressing_hides_the_tooltip()
    {
        _frames.Frame().MovePointer(50f, 30f).Frame();
        _frames.Wait(0.6).Frame();
        Assert.True(_frames.Drew("Tip"));

        _frames.Press().Frame();
        Assert.False(_frames.Drew("Tip"));

        _frames.Release().MovePointer(300f, 200f).Frame();
        _frames.MovePointer(50f, 30f).Frame();
        Assert.False(_frames.Drew("Tip"));
    }

    [Fact]
    public void A_tooltip_scope_records_its_own_content_once_open()
    {
        var opened = new List<bool>();
        _frames.Body = ui =>
        {
            using var screen = ui.Screen();
            var b = ui.Interact("b");
            ui.Leaf(b.Id, new Box { Width = 100f, Height = 30f });
            using var tooltip = ui.TooltipContent(b.Id);
            opened.Add(tooltip.IsOpen);
            if (!tooltip.IsOpen) return;
            ui.Text("Header", bold: true);
            ui.Text("Line");
        };

        _frames.Frame().MovePointer(50f, 30f).Frame();
        _frames.Wait(0.6).Frame();

        Assert.Equal([false, false, true], opened);
        Assert.True(_frames.Drew("Header"));
        Assert.True(_frames.Drew("Line"));
    }
}

public sealed class FlyoutTests
{
    private readonly UiFrames _frames;
    private bool _open;
    private int _clicks;

    public FlyoutTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        var owner = ui.Interact("owner");
        if (owner.Clicked) _open = !_open;
        ui.Leaf(owner.Id, new Box { Width = 60f, Height = 20f });
        using var flyout = ui.Flyout(ui.GetId("flyout"), ref _open, owner.Id);
        if (!flyout.IsOpen) return;
        if (ui.Button("Inside")) _clicks++;
    }

    [Fact]
    public void A_flyout_keeps_open_for_clicks_inside_and_closes_on_a_press_outside_or_Escape()
    {
        _frames.Frame().ClickOn("owner").Frame();
        Assert.True(_frames.Drew("Inside"));

        _frames.Click(_frames.TextRect("Inside")).Frame();
        Assert.Equal(1, _clicks);
        Assert.True(_open);

        _frames.Click(350f, 250f).Frame();
        Assert.False(_open);
        Assert.False(_frames.Drew("Inside"));

        _frames.ClickOn("owner").Frame();
        Assert.True(_open);
        _frames.Key(KeyboardKey.Escape).Frame();
        Assert.False(_open);
    }
}

public sealed class MenuRadioItemTests
{
    private readonly UiFrames _frames;
    private string _theme = "Dark";

    public MenuRadioItemTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var bar = ui.MenuBar("bar");
        using var menu = ui.Menu("Theme");
        if (!menu.IsOpen) return;
        foreach (var choice in new[] { "Dark", "Light" })
        {
            if (ui.MenuRadioItem(choice, _theme == choice))
                _theme = choice;
        }
    }

    [Fact]
    public void Only_the_selected_choice_shows_a_dot()
    {
        _frames.Frame().ClickOn("Theme").Frame();

        var dot = Assert.Single(_frames.Canvas.Circles);
        var row = _frames.Canvas.Texts.Single(t => t.Inputs.Text == "Dark").Inputs.Position;
        Assert.InRange(dot.Inputs.Center.Y, MathF.Min(row.Top, row.Bottom), MathF.Max(row.Top, row.Bottom));
    }

    [Fact]
    public void Picking_a_choice_selects_it()
    {
        _frames.Frame().ClickOn("Theme").Frame();

        _frames.ClickOn("Light");

        Assert.Equal("Light", _theme);
    }
}

public sealed class DropdownButtonTests
{
    private readonly UiFrames _frames;
    private readonly List<string> _picks = new();
    private bool _enabled = true;

    public DropdownButtonTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var menu = ui.DropdownButton("more", enabled: _enabled);
        if (!menu.IsOpen) return;
        if (ui.MenuItem("First")) _picks.Add("First");
        if (ui.MenuItem("Second")) _picks.Add("Second");
    }

    private void ClickButton() => _frames.ClickOn("more").Frame();

    [Fact]
    public void Clicking_opens_the_menu_below_the_button_and_a_pick_closes_it()
    {
        _frames.Frame();

        ClickButton();
        var button = _frames.RectOf("more");
        var popup = _frames.RectOf(ui => UiWidgets.MenuPopupId(ui.GetId("more")));
        Assert.True(_frames.Drew("First"));
        Assert.Equal(button.X, popup.X);
        Assert.True(popup.Y >= button.Y + button.Height);

        _frames.ClickOn("Second").Frame();

        Assert.Equal(["Second"], _picks);
        Assert.False(_frames.Drew("First"));
    }

    [Fact]
    public void Clicking_the_button_again_closes_the_menu()
    {
        _frames.Frame();
        ClickButton();

        ClickButton();

        Assert.False(_frames.Drew("First"));
        Assert.Empty(_picks);
    }

    [Fact]
    public void A_disabled_button_does_not_open()
    {
        _enabled = false;
        _frames.Frame();

        ClickButton();

        Assert.False(_frames.Drew("First"));
    }

    [Fact]
    public void Keys_open_the_menu_and_pick_from_it()
    {
        _frames.Frame().Key(KeyboardKey.Tab).Frame();

        _frames.Key(KeyboardKey.Enter).Frame(2);
        _frames.Key(KeyboardKey.DownArrow).Frame();
        _frames.Key(KeyboardKey.Enter).Frame(2);

        Assert.Equal(["Second"], _picks);
        Assert.False(_frames.Drew("First"));
    }

    [Fact]
    public void A_resting_pointer_does_not_take_the_highlight_back_from_the_keys()
    {
        _frames.Frame();
        ClickButton();
        var first = _frames.RectOf("First");
        _frames.MovePointer(first.X + 4f, first.Y + first.Height / 2).Frame();

        _frames.Key(KeyboardKey.DownArrow).Frame(2);
        _frames.Key(KeyboardKey.Enter).Frame();

        Assert.Equal(["Second"], _picks);
    }
}

public sealed class SelectTests
{
    private static readonly string[] Options = ["Apple", "Banana", "Cherry"];

    private readonly UiFrames _frames;
    private string _value = "Apple";
    private int _changes;

    public SelectTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        if (ui.Select("fruit", ref _value, Options, static s => s))
            _changes++;
    }

    [Theory]
    [InlineData("Cherry", 1)]
    [InlineData("Apple", 0)]
    public void The_button_shows_the_current_option_and_a_pick_closes_the_menu_and_changes_it_unless_current(string pick, int changes)
    {
        _frames.Frame();
        Assert.True(_frames.Drew("Apple"));
        Assert.False(_frames.Drew("Cherry"));

        _frames.ClickOn("fruit").Frame();
        Assert.True(_frames.Drew("Cherry"));

        _frames.ClickOn(pick).Frame();

        Assert.Equal(pick, _value);
        Assert.Equal(changes, _changes);
        Assert.False(_frames.Drew("Banana"));
    }
}

public sealed class SelectButtonTests
{
    private readonly UiFrames _frames;
    private bool _red = true;
    private bool _green;

    public SelectButtonTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var menu = ui.SelectButton("colors", "Colors");
        if (!menu.IsOpen) return;
        ui.MenuItem("Red", ref _red, keepOpen: true);
        ui.MenuItem("Green", ref _green, keepOpen: true);
        ui.MenuItem("Blue", ref _red, keepOpen: true, icon: new UiIcon("B", Color: 0xFF0000FF));
    }

    [Fact]
    public void A_check_items_icon_sits_between_the_check_and_the_label_whether_checked_or_not()
    {
        _frames.Frame().ClickOn("colors").Frame();
        Assert.True(IsLeftOf("B", "Blue"));
        Assert.True(IsLeftOf("Red", "Blue"));

        _frames.ClickOn("Blue").Frame();

        Assert.False(_red);
        Assert.Equal(0xFF0000FFu, _frames.TextColor("B"));
        Assert.True(IsLeftOf("B", "Blue"));
    }

    private bool IsLeftOf(string left, string right) => _frames.TextRect(left).Right <= _frames.TextRect(right).X;

    [Fact]
    public void Kept_open_check_items_flip_one_after_another_without_closing_the_menu()
    {
        _frames.Frame().ClickOn("colors").Frame();

        _frames.ClickOn("Red").Frame();
        _frames.ClickOn("Green").Frame();

        Assert.False(_red);
        Assert.True(_green);
        Assert.True(_frames.Drew("Red"));

        _frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.False(_frames.Drew("Red"));
    }
}

public sealed class SplitIconButtonTests
{
    private readonly UiFrames _frames;
    private readonly List<string> _events = new();
    private int _revision;
    private bool _nextFocused;

    public SplitIconButtonTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using (ui.Row(new Box { Gap = 0f }))
        {
            using (var split = ui.SplitIconButton("go", "G", revision: _revision))
            {
                if (split.Clicked) _events.Add("clicked");
                if (split.IsOpen)
                {
                    if (ui.MenuItem("First")) _events.Add("First");
                    if (ui.MenuItem("Second")) _events.Add("Second");
                }
            }
            _nextFocused = ui.TabStop(ui.GetId("next"));
            ui.Leaf(ui.GetId("next"), new Box { Width = 40f, Height = 20f });
        }
    }

    private static int Arrow(Ui ui) => UiWidgets.SplitArrowId(ui.GetId("go"));

    [Fact]
    public void Clicking_the_main_half_clicks_without_opening_the_menu()
    {
        _frames.Frame();

        _frames.ClickOn("go").Frame();

        Assert.Equal(["clicked"], _events);
        Assert.False(_frames.Drew("First"));
    }

    [Fact]
    public void Clicking_the_arrow_opens_the_menu_below_the_whole_control()
    {
        _frames.Frame();

        _frames.ClickOn(Arrow).Frame();
        var shell = _frames.RectOf(ui => UiWidgets.SplitShellId(ui.GetId("go")));
        var main = _frames.RectOf("go");
        var popup = _frames.RectOf(ui => UiWidgets.SplitMenuPopupId(ui.GetId("go")));
        _frames.ClickOn("First").Frame();

        Assert.Equal(main.X - UiTheme.Dark.BorderWidth, shell.X);
        Assert.Equal(shell.X, popup.X);
        Assert.True(popup.Y >= shell.Y + shell.Height);
        Assert.Equal(["First"], _events);
        Assert.False(_frames.Drew("First"));
    }

    [Fact]
    public void Down_opens_the_menu_of_the_focused_control_and_Enter_clicks_it()
    {
        _frames.Frame().Key(KeyboardKey.Tab).Frame();

        _frames.Key(KeyboardKey.Enter).Frame();
        _frames.Key(KeyboardKey.DownArrow).Frame(2);
        _frames.Key(KeyboardKey.Enter).Frame(2);

        Assert.Equal(["clicked", "First"], _events);
    }

    [Fact]
    public void A_change_of_revision_closes_the_open_menu()
    {
        _frames.Frame().ClickOn(Arrow).Frame();
        Assert.True(_frames.Drew("First"));

        _revision++;
        _frames.Frame();

        Assert.False(_frames.Drew("First"));
        Assert.Empty(_events);
    }

    [Fact]
    public void Tab_closes_the_menu_and_moves_focus_on_from_the_control()
    {
        _frames.Frame().Key(KeyboardKey.Tab).Frame();
        _frames.Key(KeyboardKey.DownArrow).Frame(2);
        Assert.True(_frames.Drew("First"));

        _frames.Key(KeyboardKey.Tab).Frame(4);

        Assert.False(_frames.Drew("First"));
        Assert.True(_nextFocused);
        Assert.Empty(_events);
    }
}

public sealed class MenuItemIconTests
{
    private readonly UiFrames _frames = new(body: Body);

    private static void Body(Ui ui)
    {
        using var menu = ui.DropdownButton("more");
        if (!menu.IsOpen) return;
        ui.MenuItem("Plain");
        ui.MenuItem("Iconic", icon: new UiIcon("I", Color: 0xFF00FF00, Overlay: "o"));
        ui.MenuItem("Checked", check: true, icon: new UiIcon("X"));
    }

    [Fact]
    public void An_icon_sits_in_the_mark_column_with_its_overlay_unless_the_item_is_checked()
    {
        _frames.Frame().ClickOn("more").Frame();

        Assert.True(_frames.Drew("I"));
        Assert.Equal(0xFF00FF00u, _frames.TextColor("I"));
        Assert.True(_frames.Drew("o"));
        Assert.False(_frames.Drew("X"));
    }
}

public sealed class MenuScrollTests
{
    private const int ItemCount = 60;

    private readonly UiFrames _frames = new(height: 2400f, body: Body);

    private static void Body(Ui ui)
    {
        using var menu = ui.DropdownButton("more");
        if (!menu.IsOpen) return;
        for (var i = 0; i < ItemCount; i++)
            ui.MenuItem($"Item {i}");
    }

    [Fact]
    public void A_long_menu_stops_at_the_theme_height_and_End_scrolls_its_last_item_into_view()
    {
        _frames.Frame().ClickOn("more").Frame();
        var popup = _frames.RectOf(ui => UiWidgets.MenuPopupId(ui.GetId("more")));
        var theme = UiTheme.Dark;
        var chrome = (theme.MenuPadding + theme.BorderWidth) * 2;

        _frames.Key(KeyboardKey.End).Frame(3);
        var last = _frames.RectOf($"Item {ItemCount - 1}");

        Assert.True(popup.Height <= theme.MenuMaxHeight + chrome, $"{popup.Height}");
        Assert.True(last.Y >= popup.Y && last.Bottom <= popup.Bottom, $"{last} outside {popup}");
    }
}

public sealed class IconOverlayTests
{
    [Fact]
    public void An_icons_overlay_is_clipped_with_its_row_by_the_list_it_scrolls_in()
    {
        var frames = new UiFrames(body: ui =>
        {
            using var screen = ui.Screen();
            using (ui.Scroll("list", new Box { Height = 100f, Gap = 0f }))
            {
                for (var i = 0; i < 30; i++)
                {
                    using (ui.Row(new Box { Height = 24f }))
                    {
                        ui.Icon(new UiIcon("I", Overlay: "o"));
                        ui.Text($"row {i}");
                    }
                }
            }
        }).Frame(2);
        var list = frames.RectOf("list");

        var overlays = frames.TextRects().Where(t => t.Text == "o").ToList();

        Assert.InRange(overlays.Count, 1, 6);
        Assert.All(overlays, t => Assert.True(t.Rect.Y <= list.Bottom, $"{t.Rect.Y} > {list.Bottom}"));
    }
}

public sealed class NestedScrollbarTests
{
    private readonly UiFrames _frames;
    private float _outerX;
    private uint _thumb;

    public NestedScrollbarTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        _thumb = ui.Theme.ScrollThumb;
        float y = 0f;
        using (ui.Scroll(ui.GetId("outer"), ScrollAxes.Horizontal, new Box { Width = 200f, Height = 120f }))
        using (ui.Box(new Box { Width = 600f, Height = 100f, ScrollX = 0f }))
        {
            var x = 0f;
            using var inner = ui.VirtualScroll(ui.GetId("inner"), 3, 20f, ref x, ref y, new Box { Width = 600f, Height = 100f, Gap = 0f });
            for (var i = inner.First; i < inner.End; i++)
                ui.Leaf(new Box { Width = 2000f, Height = 20f });
        }
        ui.ScrollTo(ui.GetId("outer"), _outerX, 0f);
    }

    [Fact]
    public void A_scroll_inside_a_scrolled_one_keeps_its_bar_within_the_outer_view()
    {
        _frames.Frame();
        _outerX = 100f;
        _frames.Frame(2);
        var outer = _frames.RectOf("outer");
        var thumbs = _frames.Canvas.Rects.Where(r => r.Inputs.Style.BackgroundColor == _thumb).ToList();

        Assert.NotEmpty(thumbs);
        Assert.All(thumbs, r =>
        {
            Assert.NotNull(r.Clip);
            Assert.True(r.Clip!.Value.Left >= outer.X - 0.5f && r.Clip.Value.Right <= outer.Right + 0.5f, $"{r.Clip} outside {outer}");
        });
    }
}
