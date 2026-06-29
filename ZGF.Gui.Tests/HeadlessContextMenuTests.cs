using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>Proves context menus — separate windows in the live app — are fully driveable headlessly
/// through <see cref="HeadlessContextMenuHost"/>: they open into the harness, show up in the window
/// forest, and a click runs the real item controller (dismissing on a leaf, staying open when
/// disabled).</summary>
public class HeadlessContextMenuTests
{
    // Builds the menu against the popup context the host supplies — its input system owns the item
    // controllers, exactly as production BuildMenu does.
    private static ContextMenu TwoItemMenu(Context ctx, Action onFetch, Action onPull)
    {
        var input = ctx.Require<InputSystem>();
        var menu = new ContextMenu();

        var fetch = new ContextMenuItem(ctx.Canvas) { Text = "Fetch" };
        fetch.UseController(input, () => new ContextMenuItemDefaultKbmController(fetch, ctx, onFetch));
        menu.Children.Add(fetch);

        var pull = new ContextMenuItem(ctx.Canvas) { Text = "Pull" };
        pull.UseController(input, () => new ContextMenuItemDefaultKbmController(pull, ctx, onPull));
        menu.Children.Add(pull);

        return menu;
    }

    [Fact]
    public void Menu_Opens_ShowsInWindowForest_AndLeafClickRunsActionAndCloses()
    {
        using var h = GuiTestHarness.Create(_ => new ColumnView());
        var host = h.Context.Require<IContextMenuHost>();

        var fetched = 0;
        var pulled = 0;
        host.ShowContextMenu(ctx => TwoItemMenu(ctx, () => fetched++, () => pulled++), default);

        Assert.Equal(1, h.OpenMenuCount);

        var forest = h.SnapshotWindows().ToText();
        Assert.Contains("=== window: main", forest);
        Assert.Contains("=== window: context-menu", forest);
        Assert.Contains("Fetch", forest);
        Assert.Contains("Pull", forest);

        h.ClickMenuItem("Fetch");

        Assert.Equal(1, fetched);
        Assert.Equal(0, pulled);
        Assert.Equal(0, h.OpenMenuCount); // leaf click dismissed the whole menu
    }

    [Fact]
    public void DisabledItem_ConsumesClick_WithoutActivating_AndKeepsMenuOpen()
    {
        using var h = GuiTestHarness.Create(_ => new ColumnView());
        var host = h.Context.Require<IContextMenuHost>();

        var clicked = 0;
        host.ShowContextMenu(ctx =>
        {
            var input = ctx.Require<InputSystem>();
            var menu = new ContextMenu();
            var item = new ContextMenuItem(ctx.Canvas) { Text = "Nope", IsEnabled = false };
            item.UseController(input, () => new ContextMenuItemDefaultKbmController(item, ctx, () => clicked++));
            menu.Children.Add(item);
            return menu;
        }, default);

        h.ClickMenuItem("Nope");

        Assert.Equal(0, clicked);
        Assert.Equal(1, h.OpenMenuCount); // disabled press is consumed; the menu stays open
    }
}
