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
