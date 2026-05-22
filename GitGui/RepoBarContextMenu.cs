using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

public readonly record struct MenuLabelSegment(string Text, uint? Color = null, bool Bold = false);

public static class RepoBarContextMenu
{
    public sealed record Item(
        string Label,
        Action OnSelected,
        string? Icon = null,
        bool Enabled = true,
        IReadOnlyList<MenuLabelSegment>? LabelSegments = null);

    public static IOpenedContextMenu? Show(Context context, PointF anchor, IReadOnlyList<Item> items)
    {
        ZGF.Gui.PopupDebugLog.Log(ZGF.Gui.PopupDebugLog.Channel.Lifecycle,
            $"RepoBarContextMenu.Show: anchor={anchor} items={items.Count}");
        if (items.Count == 0) return null;
        var manager = context.Get<ContextMenuManager>();
        if (manager == null) return null;

        manager.CloseAllImmediately();

        var menu = new ContextMenu
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = BorderColorStyle.All(DialogPalette.Border),
            BorderSize = BorderSizeStyle.All(1),
            Padding = PaddingStyle.All(4),
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
            };

            if (item.LabelSegments is { Count: > 0 } segs)
                menuItem.SetLabelView(BuildSegmentsView(segs, item.Enabled));

            var captured = item;
            menuItem.UseController(ctx => new ContextMenuItemDefaultKbmController(menuItem, ctx, () =>
            {
                manager.RequestCloseMenu(menu);
                captured.OnSelected();
            }));
            menu.Children.Add(menuItem);
        }

        var opened = manager.ShowContextMenu(menu, anchor);
        if (opened == null) return null;

        menu.UseController(_ => new ContextMenuKbmController(opened));
        return opened;
    }

    private static MultiChildView BuildSegmentsView(IReadOnlyList<MenuLabelSegment> segments, bool enabled)
    {
        var row = new FlexRowView
        {
            Gap = 0,
            CrossAxisAlignment = CrossAxisAlignment.Center,
        };
        foreach (var seg in segments)
        {
            var tv = new TextView
            {
                Text = seg.Text,
                VerticalTextAlignment = TextAlignment.Center,
            };
            if (seg.Color.HasValue && enabled)
                tv.TextColor = seg.Color.Value;
            else
                tv.TextColor = enabled ? DialogPalette.RowText : DialogPalette.RowTextMissing;
            if (seg.Bold && enabled)
                tv.FontWeight = FontWeight.Bold;
            row.Children.Add(tv);
        }
        return row;
    }
}
