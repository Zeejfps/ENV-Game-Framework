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
        Toolbar(ui, w);
        ui.Separator();
        if (w.Tree.Roots.Count == 0)
            EmptyHint(ui);
        var result = ui.TreeView(TreeId, w.Tree.Roots, w.Tree, w.Tree.View, new Box { Padding = new Spacing(0f, theme.GapTight) });
        AnalyzeOnShortcut(ui, w);
        CloseOnDelete(ui, w);
        Respond(ui, w, result);
        ContextMenu(ui, w);
    }

    private static void Toolbar(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var any = w.Tree.Roots.Count > 0;
        using (ui.PaneToolbar())
        {
            if (ui.IconButton("collapse", LucideIcons.ChevronsDownUp, LucideIcons.Family, t.CodeCollapseAll, any))
                w.Tree.CollapseAll();
            if (ui.IconButton("expand", LucideIcons.ChevronsUpDown, LucideIcons.Family, t.TreeExpandNamespaces, any))
                w.Tree.ExpandNamespaces();
            ui.Separator(theme.IconSize);
            var picked = FindPickedAssembly(w);
            if (ui.IconButton("close-assembly", LucideIcons.Trash2, LucideIcons.Family, t.TreeCloseAssembly, picked != null))
                w.Close(picked!.Assembly);
        }
    }

    /// <summary>The assembly node selected in the tree, or null when none is or another kind of node is.</summary>
    private static BrowseNode? FindPickedAssembly(Workspace w)
    {
        var node = w.Tree.View.Selected;
        if (node == null)
            return null;
        if (node.Kind != NodeKind.Assembly)
            return null;
        return node;
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

    private static void CloseOnDelete(Ui<UiState> ui, Workspace w)
    {
        if (!ui.HasFocus(ui.GetId(TreeId)))
            return;
        var picked = FindPickedAssembly(w);
        if (picked == null)
            return;
        if (!Shortcuts.DeletePressed(ui))
            return;
        w.Close(picked.Assembly);
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
        if (OffersSaveCode(node) && ui.MenuItem(t.FileSaveCode, Shortcuts.SaveCode, enabled: w.CanSaveCode(node), id: "save-code", icon: SaveDialogs.SaveCodeIcon))
            SaveDialogs.PickCodeFile(ui, w, node);
        if (node.Kind == NodeKind.Assembly && ui.MenuItem(t.FileExport, id: "export", icon: SaveDialogs.ExportIcon))
            w.OpenExportSetup(node.Assembly);
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

    /// <summary>Whether the menu offers to save the node's code: it shows code, and is no resource,
    /// whose own Export item saves its bytes.</summary>
    private static bool OffersSaveCode(BrowseNode node)
    {
        if (node.HasResource)
            return false;
        return node.DependsOnLanguage;
    }

    private static void PdbItem(Ui<UiState> ui, Workspace w, LoadedAssembly assembly)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.TreeGeneratePdb, enabled: assembly.CanGeneratePdb, id: "generate-pdb", icon: SaveDialogs.PdbIcon))
            SaveDialogs.PickPdbFile(ui, w, assembly);
    }
}
