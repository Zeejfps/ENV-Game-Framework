using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>Where the tests find a menu's items, which are keyed inside their menu rather than beside
/// the widget that opens it. Each takes the owner as recorded at the top of the frame.</summary>
internal static class MenuItems
{
    /// <summary><paramref name="item"/> in the menu that the title, dropdown, select or submenu item
    /// <paramref name="owner"/> opens.</summary>
    public static int In(Ui ui, string owner, string item) => UiWidgets.MenuItemId(UiWidgets.MenuPopupId(ui.GetId(owner)), item);

    /// <summary><paramref name="item"/> in the submenu that the item <paramref name="opener"/> of the
    /// menu <paramref name="owner"/> opens.</summary>
    public static int InSubMenu(Ui ui, string owner, string opener, string item)
    {
        var openerId = In(ui, owner, opener);
        return UiWidgets.MenuItemId(UiWidgets.MenuPopupId(openerId), item);
    }

    /// <summary><paramref name="item"/> in the context menu <paramref name="menu"/>.</summary>
    public static int InContextMenu(Ui ui, string menu, string item) => UiWidgets.MenuItemId(ui.GetId(menu), item);

    /// <summary><paramref name="item"/> in the menu of the split button <paramref name="button"/>.</summary>
    public static int InSplitButton(Ui ui, string button, string item) =>
        UiWidgets.MenuItemId(UiWidgets.SplitMenuPopupId(ui.GetId(button)), item);
}
