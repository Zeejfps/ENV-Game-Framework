using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>A menu's items are keyed by their labels, which often come from the app's data, such as
/// branch or file names. They are keyed inside the menu, so a label that matches an id outside it
/// names the item, not the widget outside.</summary>
public sealed class MenuItemIdTests
{
    private const string DialogId = "new-task";
    private static readonly string[] Options = ["main", DialogId, "develop"];
    private readonly UiFrames _frames;
    private bool _open = true;
    private string _value = "main";

    public MenuItemIdTests() => _frames = new UiFrames(new DefaultUiState(), width: 800f, height: 600f, body: Body);

    private void Body(Ui ui)
    {
        using var dialog = ui.Dialog(DialogId, "New task", ref _open);
        if (!dialog.IsOpen)
            return;
        ui.Select("start", ref _value, Options, static s => s);
    }

    [Fact]
    public void Picking_an_option_labelled_with_the_dialogs_id_picks_it_and_leaves_the_dialog_open()
    {
        _frames.Frame();
        _frames.ClickOn("start").Frame();

        _frames.Click(_frames.TextRect(DialogId)).Frame();

        Assert.Equal(DialogId, _value);
        Assert.True(_open);
    }
}

/// <summary>An item of a context menu labelled like a button behind the menu is the item.</summary>
public sealed class ContextMenuItemIdTests
{
    private const string Shared = "save";
    private readonly UiFrames _frames;
    private int _buttonClicks;
    private int _itemPicks;

    public ContextMenuItemIdTests() => _frames = new UiFrames(new DefaultUiState(), width: 800f, height: 600f, body: Body);

    private void Body(Ui ui)
    {
        if (ui.Button(Shared))
            _buttonClicks++;
        using var menu = ui.ContextMenu("menu");
        if (!menu.IsOpen)
            return;
        if (ui.MenuItem(Shared))
            _itemPicks++;
    }

    [Fact]
    public void Picking_an_item_labelled_like_a_button_behind_the_menu_picks_the_item_only()
    {
        _frames.Run(1f / 60f, ui => ui.OpenContextMenu("menu", new PointF(300f, 300f)));
        _frames.Frame();

        var item = _frames.TextRects().Last(t => t.Text == Shared).Rect;
        _frames.Click(item).Frame();

        Assert.Equal(1, _itemPicks);
        Assert.Equal(0, _buttonClicks);
    }
}
