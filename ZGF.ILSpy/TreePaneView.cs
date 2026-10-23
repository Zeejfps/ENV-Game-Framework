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

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var theme = ui.Theme;
        w.Tree.SetStyle(theme, ui.State.NodeIconPalette);
        w.Tree.SetShowNonPublic(w.ShowNonPublic);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        if (w.Tree.Roots.Count == 0)
            EmptyHint(ui);
        var result = ui.TreeView(TreeId, w.Tree.Roots, w.Tree, w.Tree.View, new Box { Padding = new Spacing(0f, theme.GapTight) });
        AnalyzeOnShortcut(ui, w);
        Respond(ui, w, result);
        ContextMenu(ui, w);
    }

    private static void EmptyHint(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var text = ui.Memo("empty", t, static s => s.TreeEmpty(Shortcuts.Open));
        ui.Text(text, new Box { Width = Sizing.Grow(), Padding = new Spacing(theme.PanelPadding, theme.Gap) }, color: theme.TextMuted, wrap: true);
    }

    private static void AnalyzeOnShortcut(Ui<UiState> ui, Workspace w)
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

    private static void Respond(Ui<UiState> ui, Workspace w, TreeViewResult<BrowseNode> result)
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

    private static void OpenContextMenu(Ui<UiState> ui, Workspace w, BrowseNode node, PointF at)
    {
        w.MenuTarget = node;
        ui.OpenContextMenu(MenuId, at);
    }

    private static void ContextMenu(Ui<UiState> ui, Workspace w)
    {
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var node = w.MenuTarget;
        if (node == null)
            return;
        NodeMenu(ui, w, node);
    }

    private static void NodeMenu(Ui<UiState> ui, Workspace w, BrowseNode node)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.TreeOpenInNewTab, id: "open-in-new-tab"))
            w.Show(node, inNewTab: true);
        if (node.Target != null && ui.MenuItem(t.TreeGoToTarget, id: "go-to-target"))
            w.GoToTarget(node);
        if (ui.MenuItem(t.TreeCopyFullName, id: "copy-full-name"))
            ui.ClipboardText = node.FullName;
        if (Analysis.Of(node) != null && ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
            w.Analyze(node);
        if (node.HasResource && ui.MenuItem(t.ResourceExport, id: "export-resource", icon: ResourceImageView.ExportIcon))
            ResourceImageView.PickExport(ui, w, node);
        if (node.DependsOnLanguage && ui.MenuItem(t.FileSaveCode, Shortcuts.SaveCode, enabled: w.CanSaveCode(node), id: "save-code", icon: SaveDialogs.SaveCodeIcon))
            SaveDialogs.PickCodeFile(ui, w, node);
        if (node.Kind == NodeKind.Assembly)
            PdbItem(ui, w, node.Assembly);
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

    private static void PdbItem(Ui<UiState> ui, Workspace w, LoadedAssembly assembly)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.TreeGeneratePdb, enabled: assembly.CanGeneratePdb, id: "generate-pdb", icon: SaveDialogs.PdbIcon))
            SaveDialogs.PickPdbFile(ui, w, assembly);
    }
}
