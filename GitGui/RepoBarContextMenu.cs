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
        if (items.Count == 0) return null;
        var manager = context.Get<ContextMenuManager>();
        if (manager == null) return null;

        manager.CloseAllImmediately();

        // Snapshot the current tokens at open time. Menus are short-lived; live theme swap
        // during an open menu is out of scope.
        var tokens = (context.Get<IThemeService>()?.Tokens.Value) ?? ThemePresets.Dark;
        var d = tokens.Dialog;

        var menu = new ContextMenu
        {
            BackgroundColor = d.Background,
            BorderColor = BorderColorStyle.All(d.Border),
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
                SelectedBackgroundColor = d.RowHover,
                TextColor = d.RowText,
                DisabledTextColor = d.RowTextMissing,
                IsEnabled = item.Enabled,
            };

            if (item.LabelSegments is { Count: > 0 } segs)
                menuItem.SetLabelView(BuildSegmentsView(segs, item.Enabled, d));

            var captured = item;
            menuItem.UseController(ctx => new ContextMenuItemDefaultKbmController(menuItem, ctx, () =>
            {
                manager.RequestCloseMenu(menu);
                captured.OnSelected();
            }));
            menu.Children.Add(menuItem);
        }

        var coords = context.Get<IWindowCoordinates>();
        var screen = coords != null ? coords.ToScreenPoints(anchor) : default;
        var opened = manager.ShowContextMenu(menu, screen);
        if (opened == null) return null;

        menu.UseController(_ => new ContextMenuKbmController(opened));
        return opened;
    }

    private static MultiChildView BuildSegmentsView(IReadOnlyList<MenuLabelSegment> segments, bool enabled, DialogTokens d)
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
                tv.TextColor = enabled ? d.RowText : d.RowTextMissing;
            if (seg.Bold && enabled)
                tv.FontWeight = FontWeight.Bold;
            row.Children.Add(tv);
        }
        return row;
    }
}
