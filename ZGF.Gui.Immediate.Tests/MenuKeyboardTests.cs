using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MenuKeyboardTests
{
    private readonly UiFrames _frames;
    private readonly TestPopupHost? _popups;
    private readonly List<string> _picks = new();
    private int _escapesOutside;
    private int _arrowsOutside;
    private bool _listFocused;

    public MenuKeyboardTests() : this(null) { }

    private MenuKeyboardTests(TestPopupHost? popups)
    {
        _popups = popups;
        _frames = new UiFrames(popups: popups);
    }

    private void Body(Ui ui)
    {
        foreach (var stroke in ui.Input.Keys)
        {
            if (stroke.Key == KeyboardKey.Escape) _escapesOutside++;
            if (stroke.Key == KeyboardKey.DownArrow) _arrowsOutside++;
        }

        using (ui.MenuBar("bar"))
        {
            using (var file = ui.Menu("File"))
            {
                if (file.IsOpen)
                {
                    if (ui.MenuItem("Open")) _picks.Add("Open");
                    if (ui.MenuItem("Disabled", enabled: false)) _picks.Add("Disabled");
                    using (var recent = ui.SubMenu("Recent"))
                    {
                        if (recent.IsOpen)
                        {
                            if (ui.MenuItem("One")) _picks.Add("One");
                            if (ui.MenuItem("Two")) _picks.Add("Two");
                        }
                    }
                    if (ui.MenuItem("Pick…"))
                        ui.PickFile("Choose", null, null, path => _picks.Add(path));
                }
            }
            using (var view = ui.Menu("View"))
            {
                if (view.IsOpen && ui.MenuItem("Dark")) _picks.Add("Dark");
            }
        }

        var list = ui.Interact("list");
        if (list.Pressed) ui.Focus(list.Id, takesText: false);
        _listFocused = ui.HasFocus(list.Id);
        ui.Leaf(list.Id, new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
    }

    private void Frame() => _frames.Run(1f / 60f, Body);

    private void Key(KeyboardKey key)
    {
        _frames.Key(key);
        Frame();
        Frame();
    }

    private void OpenFile()
    {
        Frame();
        _frames.ClickOn(Body, ui => ui.GetId("File"));
        Frame();
        _frames.MovePointer(390f, 290f);
        Frame();
    }

    [Fact]
    public void Arrow_keys_move_through_enabled_items_and_enter_picks()
    {
        OpenFile();

        Key(KeyboardKey.DownArrow);
        Key(KeyboardKey.DownArrow);
        Key(KeyboardKey.DownArrow);
        Key(KeyboardKey.UpArrow);
        Key(KeyboardKey.Enter);

        Assert.Empty(_picks);
        Assert.True(_frames.Drew("One"));
        Key(KeyboardKey.Escape);
        Assert.False(_frames.Drew("One"));
        Assert.True(_frames.Drew("Open"));

        Key(KeyboardKey.Home);
        Key(KeyboardKey.Enter);
        Assert.Equal(["Open"], _picks);
        Assert.False(_frames.Drew("Open"));
        Assert.Equal(0, _arrowsOutside);
    }

    [Fact]
    public void Right_opens_a_submenu_on_its_first_item_and_left_closes_only_it()
    {
        OpenFile();
        Key(KeyboardKey.End);
        Key(KeyboardKey.UpArrow);

        Key(KeyboardKey.RightArrow);
        Assert.True(_frames.Drew("One"));
        Key(KeyboardKey.DownArrow);
        Key(KeyboardKey.Enter);

        Assert.Equal(["Two"], _picks);
        Assert.False(_frames.Drew("Open"));

        OpenFile();
        Key(KeyboardKey.End);
        Key(KeyboardKey.UpArrow);
        Key(KeyboardKey.RightArrow);
        Key(KeyboardKey.LeftArrow);
        Assert.False(_frames.Drew("One"));
        Assert.True(_frames.Drew("Open"));
    }

    [Fact]
    public void Left_and_right_switch_between_the_bar_menus()
    {
        OpenFile();

        Key(KeyboardKey.RightArrow);
        Assert.True(_frames.Drew("Dark"));
        Assert.False(_frames.Drew("Open"));
        Key(KeyboardKey.Enter);
        Assert.Equal(["Dark"], _picks);

        OpenFile();
        Key(KeyboardKey.LeftArrow);
        Assert.True(_frames.Drew("Dark"));
    }

    [Fact]
    public void Hovering_a_submenu_opens_it_beside_its_item()
    {
        OpenFile();
        var recent = _frames.RectOf(Body, ui => ui.GetId("Recent"));

        _frames.MovePointer(recent.X + 10f, recent.Y + 5f);
        Frame();
        Frame();
        Frame();

        Assert.True(_frames.Drew("One"));
        var one = _frames.RectOf(Body, ui => ui.GetId("One"));
        Assert.True(one.X >= recent.Right);

        _frames.ClickOn(Body, ui => ui.GetId("One"));
        Assert.Equal(["One"], _picks);
        Frame();
        Assert.False(_frames.Drew("Open"));
    }

    [Fact]
    public void Pressing_a_title_and_releasing_over_an_item_picks_it()
    {
        Frame();
        var title = _frames.RectOf(Body, ui => ui.GetId("File"));
        _frames.MovePointer(title.X + 5f, title.Y + 5f);
        Frame();
        _frames.Press();
        Frame();
        Frame();
        var open = _frames.RectOf(Body, ui => ui.GetId("Open"));

        _frames.MovePointer(open.X + 20f, open.Y + 5f);
        Frame();
        _frames.Release();
        Frame();

        Assert.Equal(["Open"], _picks);
    }

    [Fact]
    public void Escape_closes_the_menu_without_reaching_anything_else()
    {
        OpenFile();

        Key(KeyboardKey.Escape);

        Assert.False(_frames.Drew("Open"));
        Assert.Equal(0, _escapesOutside);
    }

    [Fact]
    public void Keyboard_focus_comes_back_after_a_pick()
    {
        Frame();
        _frames.Click(200f, 200f, Body);
        Assert.True(_listFocused);

        OpenFile();
        Assert.False(_listFocused);
        _frames.ClickOn(Body, ui => ui.GetId("Open"));
        Frame();
        Frame();

        Assert.Equal(["Open"], _picks);
        Assert.True(_listFocused);
    }

    [Fact]
    public void Every_popup_is_gone_before_a_native_dialog_opens_and_stays_gone()
    {
        var popups = new TestPopupHost();
        var test = new MenuKeyboardTests(popups);
        var shownAtOpen = -1;
        test._frames.FilePicker.Answer = "picked.dll";
        test._frames.FilePicker.OnOpen = () => shownAtOpen = popups.Shown.Count;

        test.OpenFile();
        Assert.NotEmpty(popups.Shown);
        test._frames.ClickOn(test.Body, ui => ui.GetId("Pick…"));
        test.Frame();
        test.Frame();

        Assert.Equal(0, shownAtOpen);
        Assert.Equal(1, popups.HideAllCount);
        Assert.Empty(popups.Shown);
        Assert.Contains("picked.dll", test._picks);
    }
}

public sealed class ContextMenuIdTests
{
    private readonly UiFrames _frames = new();
    private readonly List<string> _picks = new();

    private void Body(Ui ui)
    {
        var menuId = ui.GetId("ctx");
        using (ui.Id("deep"))
        {
            var target = ui.Interact("target");
            if (target.ClickedWith(PointerButton.Right))
                ui.OpenContextMenu(menuId);
            ui.Leaf(target.Id, new Box { Width = 200f, Height = 100f });
        }

        using var menu = ui.ContextMenu(menuId);
        if (menu.IsOpen && ui.MenuItem("Copy")) _picks.Add("Copy");
    }

    [Fact]
    public void A_context_menu_opened_by_id_from_another_scope_opens()
    {
        _frames.Run(0f, Body);

        _frames.Click(50f, 40f, Body, PointerButton.Right);
        _frames.Run(0f, Body);

        Assert.True(_frames.Drew("Copy"));
    }

    [Fact]
    public void Keys_pick_from_a_context_menu()
    {
        _frames.Run(0f, Body);
        _frames.Click(50f, 40f, Body, PointerButton.Right);
        _frames.Run(0f, Body);

        _frames.Key(KeyboardKey.DownArrow);
        _frames.Run(0f, Body);
        _frames.Key(KeyboardKey.Enter);
        _frames.Run(0f, Body);
        _frames.Run(0f, Body);

        Assert.Equal(["Copy"], _picks);
        Assert.False(_frames.Drew("Copy"));
    }
}
