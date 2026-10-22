using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class MenuKeyboardTests
{
    private readonly UiFrames _frames;
    private readonly List<string> _picks = new();
    private int _escapesOutside;
    private int _arrowsOutside;
    private bool _listFocused;

    public MenuKeyboardTests() : this(null) { }

    private MenuKeyboardTests(TestPopupHost? popups) => _frames = new UiFrames(popups: popups, body: Body) { FrameTime = 1f / 60f };

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

    private void OpenFile()
    {
        _frames.Frame().ClickOn("File").Frame();
        _frames.MovePointer(390f, 290f).Frame();
    }

    [Fact]
    public void Arrow_keys_move_through_enabled_items_and_enter_picks()
    {
        OpenFile();

        _frames.Key(KeyboardKey.DownArrow).Frame(2);
        _frames.Key(KeyboardKey.DownArrow).Frame(2);
        _frames.Key(KeyboardKey.DownArrow).Frame(2);
        _frames.Key(KeyboardKey.UpArrow).Frame(2);
        _frames.Key(KeyboardKey.Enter).Frame(2);

        Assert.Empty(_picks);
        Assert.True(_frames.Drew("One"));
        _frames.Key(KeyboardKey.Escape).Frame(2);
        Assert.False(_frames.Drew("One"));
        Assert.True(_frames.Drew("Open"));

        _frames.Key(KeyboardKey.Home).Frame(2);
        _frames.Key(KeyboardKey.Enter).Frame(2);
        Assert.Equal(["Open"], _picks);
        Assert.False(_frames.Drew("Open"));
        Assert.Equal(0, _arrowsOutside);
    }

    [Fact]
    public void Right_opens_a_submenu_on_its_first_item_and_left_closes_only_it()
    {
        OpenFile();
        _frames.Key(KeyboardKey.End).Frame(2);
        _frames.Key(KeyboardKey.UpArrow).Frame(2);

        _frames.Key(KeyboardKey.RightArrow).Frame(2);
        Assert.True(_frames.Drew("One"));
        _frames.Key(KeyboardKey.DownArrow).Frame(2);
        _frames.Key(KeyboardKey.Enter).Frame(2);

        Assert.Equal(["Two"], _picks);
        Assert.False(_frames.Drew("Open"));

        OpenFile();
        _frames.Key(KeyboardKey.End).Frame(2);
        _frames.Key(KeyboardKey.UpArrow).Frame(2);
        _frames.Key(KeyboardKey.RightArrow).Frame(2);
        _frames.Key(KeyboardKey.LeftArrow).Frame(2);
        Assert.False(_frames.Drew("One"));
        Assert.True(_frames.Drew("Open"));
    }

    [Fact]
    public void Left_and_right_switch_between_the_bar_menus()
    {
        OpenFile();

        _frames.Key(KeyboardKey.RightArrow).Frame(2);
        Assert.True(_frames.Drew("Dark"));
        Assert.False(_frames.Drew("Open"));
        _frames.Key(KeyboardKey.Enter).Frame(2);
        Assert.Equal(["Dark"], _picks);

        OpenFile();
        _frames.Key(KeyboardKey.LeftArrow).Frame(2);
        Assert.True(_frames.Drew("Dark"));
    }

    [Fact]
    public void Hovering_a_submenu_opens_it_beside_its_item()
    {
        OpenFile();
        var recent = _frames.RectOf("Recent");

        _frames.MovePointer(recent.X + 10f, recent.Y + 5f).Frame(3);

        Assert.True(_frames.Drew("One"));
        var one = _frames.RectOf("One");
        Assert.True(one.X >= recent.Right);

        _frames.ClickOn("One");
        Assert.Equal(["One"], _picks);
        _frames.Frame();
        Assert.False(_frames.Drew("Open"));
    }

    [Fact]
    public void Pressing_a_title_and_releasing_over_an_item_picks_it()
    {
        _frames.Frame();
        var title = _frames.RectOf("File");
        _frames.MovePointer(title.X + 5f, title.Y + 5f).Frame();
        _frames.Press().Frame(2);
        var open = _frames.RectOf("Open");

        _frames.MovePointer(open.X + 20f, open.Y + 5f).Frame();
        _frames.Release().Frame();

        Assert.Equal(["Open"], _picks);
    }

    [Fact]
    public void Escape_closes_the_menu_without_reaching_anything_else()
    {
        OpenFile();

        _frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.False(_frames.Drew("Open"));
        Assert.Equal(0, _escapesOutside);
    }

    [Fact]
    public void Keyboard_focus_comes_back_after_a_pick()
    {
        _frames.Frame().Click(200f, 200f);
        Assert.True(_listFocused);

        OpenFile();
        Assert.False(_listFocused);
        _frames.ClickOn("Open").Frame(2);

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
        test._frames.ClickOn("Pick…").Frame(2);

        Assert.Equal(0, shownAtOpen);
        Assert.Equal(1, popups.HideAllCount);
        Assert.Empty(popups.Shown);
        Assert.Contains("picked.dll", test._picks);
    }
}

public sealed class ContextMenuIdTests
{
    private readonly UiFrames _frames;
    private readonly List<string> _picks = new();

    public ContextMenuIdTests() => _frames = new UiFrames(body: Body);

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
        _frames.Frame().Click(50f, 40f, PointerButton.Right).Frame();

        Assert.True(_frames.Drew("Copy"));
    }

    [Fact]
    public void Keys_pick_from_a_context_menu()
    {
        _frames.Frame().Click(50f, 40f, PointerButton.Right).Frame();

        _frames.Key(KeyboardKey.DownArrow).Frame();
        _frames.Key(KeyboardKey.Enter).Frame(2);

        Assert.Equal(["Copy"], _picks);
        Assert.False(_frames.Drew("Copy"));
    }
}
