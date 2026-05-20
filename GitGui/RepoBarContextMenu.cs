using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

public static class RepoBarContextMenu
{
    public sealed record Item(string Label, Action OnSelected, string? Icon = null, bool Enabled = true);

    public static IOpenedContextMenu? Show(Context context, PointF anchor, IReadOnlyList<Item> items)
    {
        if (items.Count == 0) return null;
        var manager = context.Get<ContextMenuManager>();
        if (manager == null) return null;

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
                Icon = item.Icon,
                IconFontFamily = LucideIcons.FontFamily,
                NormalBackgroundColor = 0x00000000,
                SelectedBackgroundColor = DialogPalette.RowHover,
                TextColor = DialogPalette.RowText,
                DisabledTextColor = DialogPalette.RowTextMissing,
                IsEnabled = item.Enabled,
                ZIndex = 2001,
            };

            var captured = item;
            menuItem.UseController(ctx => new ContextMenuItemDefaultKbmController(menuItem, ctx, () =>
            {
                manager.RequestCloseMenu(menu);
                captured.OnSelected();
            }));
            menu.Children.Add(menuItem);
        }

        var opened = manager.ShowContextMenu(menu);
        if (opened == null) return null;

        menu.UseController(_ => new ContextMenuKbmController(opened));
        return opened;
    }
}
