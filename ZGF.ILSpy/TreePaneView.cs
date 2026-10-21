using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

internal static class TreePaneView
{
    public static void Render(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        w.Tree.SetTheme(theme, w.DarkMode);
        w.Tree.SetShowNonPublic(w.ShowNonPublic);
        if (w.Tree.Roots.Count == 0)
            ui.Text("Open an assembly with File › Open… (Ctrl+O)", new Box { Padding = new Spacing(10f, 8f) }, color: theme.TextMuted);

        var result = ui.TreeView("tree", w.Tree.Roots, w.Tree, w.Tree.View, new Box { Padding = new Spacing(0f, 4f) });
        if (result.SelectionChanged && result.Node is { } selected)
            w.Show(selected);
        if (result.Activated && result.Node is { Target: not null } activated)
            w.GoToTarget(activated);
        if (result.ContextMenuRequested && result.Node is { } target)
        {
            w.SetMenuTarget(target);
            ui.OpenContextMenu("tree-menu", result.ContextMenuAt);
        }

        using (var menu = ui.ContextMenu("tree-menu"))
        {
            if (menu.IsOpen && w.MenuTarget is { } node)
                NodeMenu(ui, w, node);
        }
    }

    private static void NodeMenu(Ui ui, Workspace w, BrowseNode node)
    {
        if (ui.MenuItem("Open in new tab"))
            w.Show(node, inNewTab: true);
        if (node.Target != null && ui.MenuItem("Go to target"))
            w.GoToTarget(node);
        if (ui.MenuItem("Copy full name"))
            ui.ClipboardText = node.FullName;
        if (node.Kind == NodeKind.Assembly)
        {
            ui.MenuSeparator();
            if (ui.MenuItem("Close assembly"))
                w.Close(node.Assembly);
        }
    }
}
