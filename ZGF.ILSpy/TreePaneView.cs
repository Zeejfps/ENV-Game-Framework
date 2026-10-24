using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Exports;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy;

internal static class TreePaneView
{
    private const string TreeId = "tree";
    private const string MenuId = "tree-menu";
    private const string OpenKey = "Enter";

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var theme = ui.Theme;
        var tree = ui.State.Tree;
        tree.SetStyle(theme, ui.State.NodeIconPalette);
        tree.ShowTokens(ui.State.Display.TreeTokens);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, appState);
        ui.Separator();
        if (tree.Roots.Count == 0)
            EmptyHint(ui);
        var result = ui.TreeView(TreeId, tree.Roots, tree, tree.View, new Box { Padding = new Spacing(0f, theme.GapTight) });
        AnalyzeOnShortcut(ui, appState);
        CloseOnDelete(ui, appState);
        Respond(ui, appState, result);
        ContextMenu(ui, appState);
    }

    private static void Toolbar(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var tree = ui.State.Tree;
        var any = tree.Roots.Count > 0;
        using (ui.PaneToolbar())
        {
            var opened = FindOpenedNode(appState);
            if (ui.IconButton("select-opened", LucideIcons.Crosshair, LucideIcons.Family, t.TreeSelectOpenedNode, opened != null))
                SelectOpenedNode(ui, appState, opened!);
            if (ui.IconButton("collapse", LucideIcons.ChevronsDownUp, LucideIcons.Family, t.CodeCollapseAll, any))
                tree.CollapseAll();
            if (ui.IconButton("expand", LucideIcons.ChevronsUpDown, LucideIcons.Family, t.TreeExpandNamespaces, any))
                tree.ExpandNamespaces();
            ui.Separator(theme.IconSize);
            var picked = FindPickedAssembly(tree);
            if (ui.IconButton("close-assembly", LucideIcons.Trash2, LucideIcons.Family, t.TreeCloseAssembly, picked != null))
                CloseAssembly(appState, picked!.Assembly);
        }
    }

    /// <summary>The node the active tab shows, or null when it shows none.</summary>
    private static BrowseNode? FindOpenedNode(AppState appState)
    {
        var tab = appState.Tabs.Active;
        if (tab == null)
            return null;
        return tab.Node;
    }

    /// <summary>Expands the tree down to <paramref name="node"/>, when its assembly is listed, selects it,
    /// scrolls to it and focuses the tree.</summary>
    private static void SelectOpenedNode(Ui<UiState> ui, AppState appState, BrowseNode node)
    {
        if (!appState.IsListed(node))
            return;
        ui.State.Tree.Reveal(node);
        ui.Focus(ui.GetId(TreeId), takesText: false);
    }

    /// <summary>The assembly node selected in the tree, loaded or failed, or null when none is or
    /// another kind of node is.</summary>
    private static BrowseNode? FindPickedAssembly(BrowseTree tree)
    {
        var node = tree.View.Selected;
        if (node == null)
            return null;
        if (!IsAssemblyRoot(node))
            return null;
        return node;
    }

    private static bool IsAssemblyRoot(BrowseNode node)
    {
        if (node.Kind == NodeKind.Assembly)
            return true;
        return node.Kind == NodeKind.FailedAssembly;
    }

    private static void EmptyHint(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var text = ui.Memo("empty", t, static s => s.TreeEmpty(Shortcuts.Open));
        ui.Text(text, new Box { Width = Sizing.Grow(), Padding = new Spacing(theme.PanelPadding, theme.Gap) }, color: theme.TextMuted, wrap: true);
    }

    private static void AnalyzeOnShortcut(Ui<UiState> ui, AppState appState)
    {
        if (!ui.HasFocus(ui.GetId(TreeId)))
            return;
        if (!Shortcuts.AnalyzePressed(ui))
            return;
        var picked = ui.State.Tree.View.Selected;
        if (picked == null)
            return;
        AnalyzerPanelView.Analyze(ui.State, appState, picked);
    }

    private static void CloseOnDelete(Ui<UiState> ui, AppState appState)
    {
        if (!ui.HasFocus(ui.GetId(TreeId)))
            return;
        var picked = FindPickedAssembly(ui.State.Tree);
        if (picked == null)
            return;
        if (!Shortcuts.DeletePressed(ui))
            return;
        CloseAssembly(appState, picked.Assembly);
    }

    private static void Respond(Ui<UiState> ui, AppState appState, TreeViewResult<BrowseNode> result)
    {
        var node = result.Node;
        if (node == null)
            return;
        if (result.Activated)
            Open(appState, node);
        if (result.ContextMenuRequested)
            OpenContextMenu(ui, appState, node, result.ContextMenuAt);
    }

    /// <summary>Goes where <paramref name="node"/> points when it stands for a reference, else shows it in the active tab.</summary>
    private static void Open(AppState appState, BrowseNode node)
    {
        if (node.Target != null)
        {
            GoToTarget(appState, node);
            return;
        }
        ShowNode(appState, node);
    }

    private static void OpenContextMenu(Ui<UiState> ui, AppState appState, BrowseNode node, PointF at)
    {
        ui.State.MenuTarget = node;
        ui.OpenContextMenu(MenuId, at);
    }

    private static void ContextMenu(Ui<UiState> ui, AppState appState)
    {
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var node = ui.State.MenuTarget;
        if (node == null)
            return;
        NodeMenu(ui, appState, node);
    }

    private static void NodeMenu(Ui<UiState> ui, AppState appState, BrowseNode node)
    {
        var t = ui.State.Strings;
        var pointsElsewhere = node.Target != null;
        var openKey = pointsElsewhere ? null : OpenKey;
        if (ui.MenuItem(t.TreeOpen, openKey, id: "open"))
            ShowNode(appState, node);
        if (ui.MenuItem(t.TreeOpenInNewTab, id: "open-in-new-tab"))
            ShowNode(appState, node, inNewTab: true);
        if (pointsElsewhere && ui.MenuItem(t.TreeGoToTarget, OpenKey, id: "go-to-target"))
            GoToTarget(appState, node);
        if (ui.MenuItem(t.TreeCopyFullName, id: "copy-full-name"))
            ui.ClipboardText = node.FullName;
        if (Analyzers.Of(node) != null && ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
            AnalyzerPanelView.Analyze(ui.State, appState, node);
        if (node.HasResource && ui.MenuItem(t.ResourceExport, id: "export-resource", icon: ResourceImageView.ExportIcon))
            ResourceImageView.PickExport(ui, appState, node);
        if (OffersSaveCode(node) && ui.MenuItem(t.FileSaveCode, Shortcuts.SaveCode, enabled: CanSaveCode(appState, node), id: "save-code", icon: SaveDialogs.SaveCodeIcon))
            SaveDialogs.PickCodeFile(ui, appState, node);
        if (node.Kind == NodeKind.Assembly && ui.MenuItem(t.FileExport, id: "export", icon: SaveDialogs.ExportIcon))
            ui.State.ExportSetup.Open(appState.Assemblies.Assemblies, node.Assembly);
        if (node.Kind == NodeKind.Assembly)
            PdbItem(ui, appState, node.Assembly);
        if (node.Kind == NodeKind.Assembly && ui.MenuItem(t.TreeScopeSearchToAssembly, id: "scope-search"))
            ScopeSearch(ui.State, node);
        if (node.Kind == NodeKind.Namespace && ui.MenuItem(t.TreeScopeSearchToNamespace, id: "scope-search"))
            ScopeSearch(ui.State, node);
        if (IsAssemblyRoot(node))
        {
            ui.MenuSeparator();
            if (ui.MenuItem(t.TreeCloseAssembly, id: "close-assembly"))
                CloseAssembly(appState, node.Assembly);
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

    private static void PdbItem(Ui<UiState> ui, AppState appState, LoadedAssembly assembly)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.TreeGeneratePdb, enabled: assembly.CanGeneratePdb, id: "generate-pdb", icon: SaveDialogs.PdbIcon))
            SaveDialogs.PickPdbFile(ui, appState, assembly);
    }
}
