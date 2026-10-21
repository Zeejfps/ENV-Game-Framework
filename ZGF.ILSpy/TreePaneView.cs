using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

internal static class TreePaneView
{
    public static void Render(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        w.Tree.SetStyle(theme, ui.Get<NodeIconPalette>() ?? NodeIconPalette.Dark);
        w.Tree.SetShowNonPublic(w.ShowNonPublic);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Header(ui, w);
        if (w.Tree.Roots.Count == 0)
            ui.Text("Open an assembly with File › Open… (Ctrl+O)", new Box { Width = Sizing.Grow(), Padding = new Spacing(theme.PanelPadding, theme.Gap) },
                color: theme.TextMuted, wrap: true);

        var result = ui.TreeView("tree", w.Tree.Roots, w.Tree, w.Tree.View, new Box { Padding = new Spacing(0f, theme.GapTight) });
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

    private static void Header(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box
        {
            Height = theme.PaneHeaderHeight,
            Padding = new Spacing(theme.ShellPaddingX, 0f),
            Gap = theme.GapTight,
            AlignY = Align.Center,
            Background = theme.Surface,
        }))
        {
            ui.Text("Assemblies", bold: true);
            if (w.Tree.Roots.Count > 0)
                ui.Text(ui.Memo("tree-count", w.Tree.Roots.Count, static n => n.ToString("N0")), color: theme.TextMuted, size: theme.FontSizeSmall);
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
