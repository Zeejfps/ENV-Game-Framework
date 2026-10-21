using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MenuTests
{
    private readonly UiFrames _frames = new();
    private readonly List<string> _picks = new();
    private bool _dark;
    private int _underClicks;

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

    private void OpenFile()
    {
        _frames.Run(0f, Body);
        _frames.ClickOn(Body, ui => ui.GetId("File"));
        _frames.Run(0f, Body);
    }

    private bool Open(string item)
    {
        _frames.Run(0f, Body);
        return _frames.Drew(item);
    }

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

        _frames.ClickOn(Body, ui => ui.GetId("Open"));

        Assert.Equal(["Open"], _picks);
        Assert.False(Open("Open"));
        Assert.Equal(0, _underClicks);
    }

    [Fact]
    public void A_disabled_item_cannot_be_picked()
    {
        OpenFile();

        _frames.ClickOn(Body, ui => ui.GetId("Save"));

        Assert.Empty(_picks);
        Assert.True(Open("Open"));
    }

    [Fact]
    public void Hovering_another_title_switches_the_open_menu()
    {
        OpenFile();
        var view = _frames.RectOf(Body, ui => ui.GetId("View"));

        _frames.MovePointer(view.X + 5f, view.Y + 5f);
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);

        Assert.True(Open("Dark"));
        Assert.False(_frames.Drew("Open"));
    }

    [Fact]
    public void Escape_closes_the_menu()
    {
        OpenFile();

        _frames.Key(KeyboardKey.Escape);
        _frames.Run(0f, Body);

        Assert.False(Open("Open"));
    }

    [Fact]
    public void A_press_outside_closes_the_menu()
    {
        OpenFile();

        _frames.Click(350f, 250f, Body);

        Assert.False(Open("Open"));
        Assert.Equal(1, _underClicks);
    }

    [Fact]
    public void A_dismissal_from_the_platform_closes_the_menu()
    {
        OpenFile();

        _frames.DismissPopups();
        _frames.Run(0f, Body);

        Assert.False(Open("Open"));
    }

    [Fact]
    public void A_checkable_item_flips_its_value()
    {
        _frames.Run(0f, Body);
        _frames.ClickOn(Body, ui => ui.GetId("View"));
        _frames.Run(0f, Body);

        _frames.ClickOn(Body, ui => ui.GetId("Dark"));

        Assert.True(_dark);
    }
}

public sealed class ContextMenuTests
{
    private readonly UiFrames _frames = new();
    private readonly List<string> _picks = new();

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
        _frames.Run(0f, Body);

        _frames.Click(50f, 40f, Body, PointerButton.Right);
        _frames.Run(0f, Body);
        var rect = _frames.RectOf(Body, ui => ui.GetId("ctx"));
        Assert.Equal((50f, 40f), (rect.X, rect.Y));

        _frames.ClickOn(Body, ui => ui.GetId("Copy"));
        _frames.Run(0f, Body);

        Assert.Equal(["Copy"], _picks);
        Assert.False(_frames.Drew("Copy"));
    }

    [Fact]
    public void A_press_outside_closes_the_menu()
    {
        _frames.Run(0f, Body);
        _frames.Click(50f, 40f, Body, PointerButton.Right);
        _frames.Run(0f, Body);
        Assert.True(_frames.Drew("Copy"));

        _frames.Click(350f, 250f, Body);

        Assert.False(_frames.Drew("Copy"));
        Assert.Empty(_picks);
    }
}

public sealed class TooltipTests
{
    private readonly UiFrames _frames = new();

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
        _frames.Run(0f, Body);
        _frames.MovePointer(50f, 30f);
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);

        Assert.False(_frames.Drew("Tip"));
        Assert.Equal(_frames.RealTime + 0.5, _frames.WakeAt, 3);

        _frames.Wait(0.6);
        _frames.Run(0f, Body);

        Assert.True(_frames.Drew("Tip"));
        var tip = _frames.Canvas.Texts.Single(t => t.Inputs.Text == "Tip").Inputs.Position;
        Assert.True(300f - tip.Top >= 46f + 6f);
    }

    [Fact]
    public void Leaving_or_pressing_hides_the_tooltip()
    {
        _frames.Run(0f, Body);
        _frames.MovePointer(50f, 30f);
        _frames.Run(0f, Body);
        _frames.Wait(0.6);
        _frames.Run(0f, Body);
        Assert.True(_frames.Drew("Tip"));

        _frames.Press();
        _frames.Run(0f, Body);
        Assert.False(_frames.Drew("Tip"));
        _frames.Release();

        _frames.MovePointer(300f, 200f);
        _frames.Run(0f, Body);
        _frames.MovePointer(50f, 30f);
        _frames.Run(0f, Body);
        Assert.False(_frames.Drew("Tip"));
    }
}

public sealed class MenuRadioItemTests
{
    private readonly UiFrames _frames = new();
    private string _theme = "Dark";

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

    private void OpenMenu()
    {
        _frames.Run(0f, Body);
        _frames.ClickOn(Body, ui => ui.GetId("Theme"));
        _frames.Run(0f, Body);
    }

    [Fact]
    public void Only_the_selected_choice_shows_a_dot()
    {
        OpenMenu();

        var dot = Assert.Single(_frames.Canvas.Circles);
        var row = _frames.Canvas.Texts.Single(t => t.Inputs.Text == "Dark").Inputs.Position;
        Assert.InRange(dot.Inputs.Center.Y, MathF.Min(row.Top, row.Bottom), MathF.Max(row.Top, row.Bottom));
    }

    [Fact]
    public void Picking_a_choice_selects_it()
    {
        OpenMenu();

        _frames.ClickOn(Body, ui => ui.GetId("Light"));

        Assert.Equal("Light", _theme);
    }
}

public sealed class DropdownButtonTests
{
    private readonly UiFrames _frames = new();
    private readonly List<string> _picks = new();
    private bool _enabled = true;

    private void Body(Ui ui)
    {
        using var menu = ui.DropdownButton("more", enabled: _enabled);
        if (!menu.IsOpen) return;
        if (ui.MenuItem("First")) _picks.Add("First");
        if (ui.MenuItem("Second")) _picks.Add("Second");
    }

    private void ClickButton()
    {
        _frames.ClickOn(Body, ui => ui.GetId("more"));
        _frames.Run(0f, Body);
    }

    [Fact]
    public void Clicking_opens_the_menu_below_the_button_and_a_pick_closes_it()
    {
        _frames.Run(0f, Body);

        ClickButton();
        var button = _frames.RectOf(Body, ui => ui.GetId("more"));
        var popup = _frames.RectOf(Body, ui => HashCode.Combine(ui.GetId("more"), 0x4D454E55));
        Assert.True(_frames.Drew("First"));
        Assert.Equal(button.X, popup.X);
        Assert.True(popup.Y >= button.Y + button.Height);

        _frames.ClickOn(Body, ui => ui.GetId("Second"));
        _frames.Run(0f, Body);

        Assert.Equal(["Second"], _picks);
        Assert.False(_frames.Drew("First"));
    }

    [Fact]
    public void Clicking_the_button_again_closes_the_menu()
    {
        _frames.Run(0f, Body);
        ClickButton();

        ClickButton();

        Assert.False(_frames.Drew("First"));
        Assert.Empty(_picks);
    }

    [Fact]
    public void A_disabled_button_does_not_open()
    {
        _enabled = false;
        _frames.Run(0f, Body);

        ClickButton();

        Assert.False(_frames.Drew("First"));
    }

    [Fact]
    public void Keys_open_the_menu_and_pick_from_it()
    {
        _frames.Run(0f, Body);
        _frames.Key(KeyboardKey.Tab);
        _frames.Run(0f, Body);

        _frames.Key(KeyboardKey.Enter);
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);
        _frames.Key(KeyboardKey.DownArrow);
        _frames.Run(0f, Body);
        _frames.Key(KeyboardKey.Enter);
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);

        Assert.Equal(["Second"], _picks);
        Assert.False(_frames.Drew("First"));
    }
}

public sealed class SplitIconButtonTests
{
    private const int ArrowSalt = 0x53504C41;
    private const int ShellSalt = 0x53504C53;
    private const int MenuSalt = 0x4D454E55;

    private readonly UiFrames _frames = new();
    private readonly List<string> _events = new();
    private int _revision;
    private bool _nextFocused;

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

    private static int Arrow(Ui ui) => HashCode.Combine(ui.GetId("go"), ArrowSalt);

    private void Settle()
    {
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);
    }

    [Fact]
    public void Clicking_the_main_half_clicks_without_opening_the_menu()
    {
        _frames.Run(0f, Body);

        _frames.ClickOn(Body, ui => ui.GetId("go"));
        _frames.Run(0f, Body);

        Assert.Equal(["clicked"], _events);
        Assert.False(_frames.Drew("First"));
    }

    [Fact]
    public void Clicking_the_arrow_opens_the_menu_below_the_whole_control()
    {
        _frames.Run(0f, Body);

        _frames.ClickOn(Body, Arrow);
        _frames.Run(0f, Body);
        var shell = _frames.RectOf(Body, ui => HashCode.Combine(ui.GetId("go"), ShellSalt));
        var main = _frames.RectOf(Body, ui => ui.GetId("go"));
        var popup = _frames.RectOf(Body, ui => HashCode.Combine(Arrow(ui), MenuSalt));
        _frames.ClickOn(Body, ui => ui.GetId("First"));
        _frames.Run(0f, Body);

        Assert.Equal(main.X - UiTheme.Dark.BorderWidth, shell.X);
        Assert.Equal(shell.X, popup.X);
        Assert.True(popup.Y >= shell.Y + shell.Height);
        Assert.Equal(["First"], _events);
        Assert.False(_frames.Drew("First"));
    }

    [Fact]
    public void Down_opens_the_menu_of_the_focused_control_and_Enter_clicks_it()
    {
        _frames.Run(0f, Body);
        _frames.Key(KeyboardKey.Tab);
        _frames.Run(0f, Body);

        _frames.Key(KeyboardKey.Enter);
        _frames.Run(0f, Body);
        _frames.Key(KeyboardKey.DownArrow);
        Settle();
        _frames.Key(KeyboardKey.Enter);
        Settle();

        Assert.Equal(["clicked", "First"], _events);
    }

    [Fact]
    public void A_change_of_revision_closes_the_open_menu()
    {
        _frames.Run(0f, Body);
        _frames.ClickOn(Body, Arrow);
        _frames.Run(0f, Body);
        Assert.True(_frames.Drew("First"));

        _revision++;
        _frames.Run(0f, Body);

        Assert.False(_frames.Drew("First"));
        Assert.Empty(_events);
    }

    [Fact]
    public void Tab_closes_the_menu_and_moves_focus_on_from_the_control()
    {
        _frames.Run(0f, Body);
        _frames.Key(KeyboardKey.Tab);
        _frames.Run(0f, Body);
        _frames.Key(KeyboardKey.DownArrow);
        Settle();
        Assert.True(_frames.Drew("First"));

        _frames.Key(KeyboardKey.Tab);
        Settle();
        Settle();

        Assert.False(_frames.Drew("First"));
        Assert.True(_nextFocused);
        Assert.Empty(_events);
    }
}

public sealed class MenuItemIconTests
{
    private readonly UiFrames _frames = new();

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
        _frames.Run(0f, Body);
        _frames.ClickOn(Body, ui => ui.GetId("more"));
        _frames.Run(0f, Body);

        Assert.True(_frames.Drew("I"));
        Assert.Equal(0xFF00FF00u, _frames.TextColor("I"));
        Assert.True(_frames.Drew("o"));
        Assert.False(_frames.Drew("X"));
    }
}

public sealed class MenuScrollTests
{
    private const int ItemCount = 60;

    private readonly UiFrames _frames = new(height: 2400f);

    private static void Body(Ui ui)
    {
        using var menu = ui.DropdownButton("more");
        if (!menu.IsOpen) return;
        for (var i = 0; i < ItemCount; i++)
            ui.MenuItem($"Item {i}");
    }

    private static int Popup(Ui ui) => HashCode.Combine(ui.GetId("more"), 0x4D454E55);

    [Fact]
    public void A_long_menu_stops_at_the_theme_height_and_End_scrolls_its_last_item_into_view()
    {
        _frames.Run(0f, Body);
        _frames.ClickOn(Body, ui => ui.GetId("more"));
        _frames.Run(0f, Body);
        var popup = _frames.RectOf(Body, Popup);
        var theme = UiTheme.Dark;
        var chrome = (theme.MenuPadding + theme.BorderWidth) * 2;

        _frames.Key(KeyboardKey.End);
        for (var i = 0; i < 3; i++)
            _frames.Run(0f, Body);
        var last = _frames.RectOf(Body, ui => ui.GetId($"Item {ItemCount - 1}"));

        Assert.True(popup.Height <= theme.MenuMaxHeight + chrome, $"{popup.Height}");
        Assert.True(last.Y >= popup.Y && last.Bottom <= popup.Bottom, $"{last} outside {popup}");
    }
}
