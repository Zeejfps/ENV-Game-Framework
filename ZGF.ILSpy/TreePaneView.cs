using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class TreePaneView
{
    private const string TreeId = "tree";
    private const string MenuId = "tree-menu";

    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var theme = ui.Theme;
        w.Tree.SetStyle(theme, ui.Get<NodeIconPalette>() ?? NodeIconPalette.Dark);
        w.Tree.SetShowNonPublic(w.ShowNonPublic);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        if (w.Tree.Roots.Count == 0)
            EmptyHint(ui, t);
        var result = ui.TreeView(TreeId, w.Tree.Roots, w.Tree, w.Tree.View, new Box { Padding = new Spacing(0f, theme.GapTight) });
        AnalyzeOnShortcut(ui, w);
        Respond(ui, w, result);
        ContextMenu(ui, w, t);
    }

    private static void EmptyHint(Ui ui, Strings t)
    {
        var theme = ui.Theme;
        var text = ui.Memo("empty", t, static s => s.TreeEmpty(Shortcuts.Open));
        ui.Text(text, new Box { Width = Sizing.Grow(), Padding = new Spacing(theme.PanelPadding, theme.Gap) }, color: theme.TextMuted, wrap: true);
    }

    private static void AnalyzeOnShortcut(Ui ui, Workspace w)
    {
        if (!ui.HasFocus(ui.GetId(TreeId)))
            return;
        if (!Shortcuts.AnalyzePressed(ui))
            return;
        var picked = w.Tree.View.Selected;
        if (picked == null)
            return;
        w.Analyze(picked);
    }

    private static void Respond(Ui ui, Workspace w, TreeViewResult<BrowseNode> result)
    {
        var node = result.Node;
        if (node == null)
            return;
        if (result.SelectionChanged)
            w.Show(node);
        if (result.Activated && node.Target != null)
            w.GoToTarget(node);
        if (result.ContextMenuRequested)
            OpenContextMenu(ui, w, node, result.ContextMenuAt);
    }

    private static void OpenContextMenu(Ui ui, Workspace w, BrowseNode node, PointF at)
    {
        w.MenuTarget = node;
        ui.OpenContextMenu(MenuId, at);
    }

    private static void ContextMenu(Ui ui, Workspace w, Strings t)
    {
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var node = w.MenuTarget;
        if (node == null)
            return;
        NodeMenu(ui, w, t, node);
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
        if (node.Kind == NodeKind.Assembly && ui.MenuItem(t.TreeScopeSearchToAssembly, id: "scope-search"))
            w.ScopeSearch(node);
        if (node.Kind == NodeKind.Namespace && ui.MenuItem(t.TreeScopeSearchToNamespace, id: "scope-search"))
            w.ScopeSearch(node);
        if (node.Kind == NodeKind.Assembly)
        {
            ui.MenuSeparator();
            if (ui.MenuItem(t.TreeCloseAssembly, id: "close-assembly"))
                w.Close(node.Assembly);
        }
    }
}
