using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

public static class RepoBarContextMenu
{
    public sealed record Item(string Label, Action OnSelected);

    public static void Show(Context context, PointF anchor, IReadOnlyList<Item> items)
    {
        if (items.Count == 0) return;
        var manager = context.Get<ContextMenuManager>();
        var inputSystem = context.Get<InputSystem>();
        if (manager == null || inputSystem == null) return;

        manager.CloseAllImmediately();

        var menu = new ContextMenu
        {
            AnchorPoint = anchor,
            BackgroundColor = DialogPalette.Background,
            BorderColor = BorderColorStyle.All(DialogPalette.Border),
            BorderSize = BorderSizeStyle.All(1),
            Padding = PaddingStyle.All(4),
            ZIndex = 2000,
        };

        foreach (var item in items)
        {
            var menuItem = new ContextMenuItem
            {
                Text = item.Label,
                NormalBackgroundColor = 0x00000000,
                SelectedBackgroundColor = DialogPalette.RowHover,
                TextColor = DialogPalette.RowText,
                ZIndex = 2001,
            };

            var captured = item;
            menuItem.Behaviors.Add(new ContextMenuItemDefaultKbmController(menuItem, () =>
            {
                manager.RequestCloseMenu(menu);
                captured.OnSelected();
            }));
            menu.Children.Add(menuItem);
        }

        var opened = manager.ShowContextMenu(menu);
        if (opened == null) return;

        var controller = new RepoBarContextMenuKbmController(menu, opened);
        inputSystem.RegisterController(menu, controller);
        opened.Closed += () => inputSystem.UnregisterController(menu);
    }
}
