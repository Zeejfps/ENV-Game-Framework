using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class TreePaneView
{
    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var theme = ui.Theme;
        w.Tree.SetStyle(theme, ui.Get<NodeIconPalette>() ?? NodeIconPalette.Dark);
        w.Tree.SetShowNonPublic(w.ShowNonPublic);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        if (w.Tree.Roots.Count == 0)
            ui.Text(ui.Memo("empty", t, static s => s.TreeEmpty("Ctrl+O")), new Box { Width = Sizing.Grow(), Padding = new Spacing(theme.PanelPadding, theme.Gap) },
                color: theme.TextMuted, wrap: true);

        var result = ui.TreeView("tree", w.Tree.Roots, w.Tree, w.Tree.View, new Box { Padding = new Spacing(0f, theme.GapTight) });
        if (ui.HasFocus(ui.GetId("tree")) && Shortcuts.AnalyzePressed(ui) && w.Tree.View.Selected is { } picked)
            w.Analyze(picked);
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
                NodeMenu(ui, w, t, node);
        }
    }

    private static void NodeMenu(Ui ui, Workspace w, Strings t, BrowseNode node)
    {
        if (ui.MenuItem(t.TreeOpenInNewTab, id: "open-in-new-tab"))
            w.Show(node, inNewTab: true);
        if (node.Target != null && ui.MenuItem(t.TreeGoToTarget, id: "go-to-target"))
            w.GoToTarget(node);
        if (ui.MenuItem(t.TreeCopyFullName, id: "copy-full-name"))
            ui.ClipboardText = node.FullName;
        if (Analysis.Of(node) != null && ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
            w.Analyze(node);
        if (node.HasResource && ui.MenuItem(t.ResourceExport, id: "export-resource", icon: ResourceImageView.ExportIcon))
            ResourceImageView.PickExport(ui, w, t, node);
        if (node.Kind == NodeKind.Assembly)
        {
            ui.MenuSeparator();
            if (ui.MenuItem(t.TreeCloseAssembly, id: "close-assembly"))
                w.Close(node.Assembly);
        }
    }
}
