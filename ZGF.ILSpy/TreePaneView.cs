using ZGF.Desktop;
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
        tree.SetStyle(theme, ui.State.NodeIconPalette, ui.State.Icons);
        tree.ShowTokens(ui.State.Display.TreeTokens);
        tree.ShowEmptyTables(ui.State.Display.ShowEmptyMetadataTables);
        tree.NestNamespaces(ui.State.Display.NestedNamespaces);
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
            if (ui.IconButton("select-opened", ui.State.Icons.AssembliesSelectOpened, t.TreeSelectOpenedNode, opened != null))
                SelectOpenedNode(ui, appState, opened!);
            if (ui.IconButton("collapse", ui.State.Icons.AssembliesCollapseAll, t.CodeCollapseAll, any))
                tree.CollapseAll();
            if (ui.IconButton("expand", ui.State.Icons.AssembliesExpandNamespaces, t.TreeExpandNamespaces, any))
                tree.ExpandNamespaces();
            ui.Separator(theme.IconSize);
            var picked = FindPickedAssembly(tree);
            if (ui.IconButton("close-assembly", ui.State.Icons.AssembliesCloseAssembly, t.TreeCloseAssembly, picked != null))
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

    /// <summary>The root selected in the tree, of an assembly, a package or a file that failed to
    /// load, or null when none is or another node is.</summary>
    private static BrowseNode? FindPickedAssembly(BrowseTree tree)
    {
        var node = tree.View.Selected;
        if (node == null)
            return null;
        if (!IsListedRoot(node))
            return null;
        return node;
    }

    /// <summary>Whether <paramref name="node"/> is the root of an item of the list; an assembly read out
    /// of a package is not one.</summary>
    private static bool IsListedRoot(BrowseNode node)
    {
        return node.Parent == null;
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
        if (IsPicked(result))
            Preview(appState, node);
        if (result.Activated)
            Open(appState, node);
        if (result.ContextMenuRequested)
            OpenContextMenu(ui, appState, node, result.ContextMenuAt);
    }

    /// <summary>Whether the user picked the node, rather than pressed it for its menu.</summary>
    private static bool IsPicked(TreeViewResult<BrowseNode> result)
    {
        if (!result.SelectionChanged)
            return false;
        return !result.ContextMenuRequested;
    }

    /// <summary>Shows <paramref name="node"/>, just picked, in the preview tab while tabs preview.</summary>
    private static void Preview(AppState appState, BrowseNode node)
    {
        if (!appState.PreviewTabs)
            return;
        PreviewNode(appState, node);
    }

    /// <summary>Goes where <paramref name="node"/> points when it stands for a reference, else opens it in a tab kept open.</summary>
    private static void Open(AppState appState, BrowseNode node)
    {
        if (node.Target != null)
        {
            GoToTarget(appState, node);
            return;
        }
        OpenNode(appState, node);
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
            OpenNode(appState, node);
        if (ui.MenuItem(t.TreeOpenInNewTab, id: "open-in-new-tab"))
            ShowNode(appState, node, inNewTab: true);
        if (pointsElsewhere && ui.MenuItem(t.TreeGoToTarget, OpenKey, id: "go-to-target"))
            GoToTarget(appState, node);
        if (ui.MenuItem(t.TreeCopyFullName, id: "copy-full-name"))
            ui.ClipboardText = node.FullName;
        DocsItem(ui, node);
        if (Analyzers.Of(node) != null && ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
            AnalyzerPanelView.Analyze(ui.State, appState, node);
        if (node.Reference != null && ui.MenuItem(t.TreeShowInMetadata, id: "show-in-metadata"))
            ShowInMetadata(appState, node.Reference, inNewTab: false);
        if (node.HasResource && ui.MenuItem(t.ResourceExport, id: "export-resource", icon: new UiIcon(ui.State.Icons.ResourceExport)))
            ResourceImageView.PickExport(ui, appState, node);
        ExtractItems(ui, appState, node);
        if (OffersSaveCode(node) && ui.MenuItem(t.FileSaveCode, Shortcuts.SaveCode, enabled: CanSaveCode(appState, node), id: "save-code", icon: new UiIcon(ui.State.Icons.FileSaveCode)))
            SaveDialogs.PickCodeFile(ui, appState, node);
        if (node.Kind == NodeKind.Assembly && ui.MenuItem(t.FileExport, id: "export", icon: new UiIcon(ui.State.Icons.FileExportProject)))
            ui.State.ExportSetup.Open(appState.Assemblies.Assemblies, node.Assembly);
        if (node.Kind == NodeKind.Assembly)
            PdbItems(ui, appState, node.Assembly);
        if (node.Kind == NodeKind.Assembly && ui.MenuItem(t.TreeCreateDiagram, enabled: node.Assembly.CanCreateDiagram, id: "create-diagram"))
            SaveDialogs.PickDiagramFolder(ui, appState, node.Assembly);
        if (node.Kind == NodeKind.Assembly)
            TargetFrameworkMenu(ui, appState, node.Assembly);
        if (node.Kind == NodeKind.Assembly && ui.MenuItem(t.TreeLoadDependencies, id: "load-dependencies"))
            OpenDependencies(appState, node.Assembly);
        if (node.Kind == NodeKind.Assembly && ui.MenuItem(t.TreeScopeSearchToAssembly, id: "scope-search"))
            ScopeSearch(ui.State, node);
        if (node.Kind == NodeKind.Namespace && ui.MenuItem(t.TreeScopeSearchToNamespace, id: "scope-search"))
            ScopeSearch(ui.State, node);
        if (IsListedRoot(node) && ui.MenuItem(t.TreeOpenContainingFolder, id: "open-containing-folder", icon: new UiIcon(ui.State.Icons.AssembliesOpenContainingFolder)))
            ui.RevealFile(node.Assembly.FilePath);
        if (IsListedRoot(node))
        {
            ui.MenuSeparator();
            if (ui.MenuItem(t.TreeCloseAssembly, id: "close-assembly"))
                CloseAssembly(appState, node.Assembly);
        }
    }

    /// <summary>The item that opens the .NET API reference page of a namespace, or of a public type or
    /// member, when it has one.</summary>
    private static void DocsItem(Ui<UiState> ui, BrowseNode node)
    {
        var t = ui.State.Strings;
        var page = MicrosoftDocs.FindPage(node);
        if (page == null)
            return;
        if (ui.MenuItem(t.TreeShowInApiReference, id: "show-in-api-reference", icon: new UiIcon(ui.State.Icons.LinkApiReference)))
            ui.OpenUrl(page);
    }

    /// <summary>The items that write files out of a package: the file of an assembly read out of one,
    /// or every file of a package or a folder of one.</summary>
    private static void ExtractItems(Ui<UiState> ui, AppState appState, BrowseNode node)
    {
        var t = ui.State.Strings;
        if (IsPackedAssembly(node) && ui.MenuItem(t.TreeExtractFile, id: "extract-file", icon: new UiIcon(ui.State.Icons.AssembliesExtract)))
            SaveDialogs.PickPackageFile(ui, appState, node.Assembly.InPackage!);
        if (node.PackageFolder != null && ui.MenuItem(t.TreeExtractAll, id: "extract-all", icon: new UiIcon(ui.State.Icons.AssembliesExtract)))
            SaveDialogs.PickExtractFolder(ui, appState, node);
    }

    /// <summary>Whether <paramref name="node"/> is the root of an assembly read out of a package.</summary>
    private static bool IsPackedAssembly(BrowseNode node)
    {
        if (node.Kind != NodeKind.Assembly)
            return false;
        return node.Assembly.InPackage != null;
    }

    /// <summary>Whether the menu offers to save the node's code: it shows code, and is no resource,
    /// whose own Export item saves its bytes.</summary>
    private static bool OffersSaveCode(BrowseNode node)
    {
        if (node.HasResource)
            return false;
        return node.DependsOnLanguage;
    }

    /// <summary>Generating a portable PDB for the assembly, and picking the PDB it reads.</summary>
    private static void PdbItems(Ui<UiState> ui, AppState appState, LoadedAssembly assembly)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.TreeGeneratePdb, enabled: assembly.CanGeneratePdb, id: "generate-pdb", icon: new UiIcon(ui.State.Icons.FileGeneratePdb)))
            SaveDialogs.PickPdbFile(ui, appState, assembly);
        if (assembly.CanPickPdb && ui.MenuItem(t.TreeSelectPdb, id: "select-pdb"))
            PickPdbToRead(ui, appState, assembly);
    }

    /// <summary>The submenu of the framework the assembly's references are resolved against: the one it
    /// was built for, or one of the common ones, the one in use picked.</summary>
    private static void TargetFrameworkMenu(Ui<UiState> ui, AppState appState, LoadedAssembly assembly)
    {
        var t = ui.State.Strings;
        using var menu = ui.SubMenu(t.TreeTargetFramework, id: "target-framework");
        if (!menu.IsOpen)
            return;
        if (ui.MenuRadioItem(DetectedFrameworkLabel(t, assembly), !assembly.IsTargetFrameworkPicked, id: "detected"))
            SetTargetFramework(appState, assembly, null);
        ui.MenuSeparator();
        foreach (var framework in FrameworkIds.Common)
        {
            if (ui.MenuRadioItem(FrameworkIds.MonikerOf(framework), IsPicked(assembly, framework), id: framework))
                SetTargetFramework(appState, assembly, framework);
        }
    }

    private static string DetectedFrameworkLabel(Strings t, LoadedAssembly assembly)
    {
        if (assembly.DetectedTargetFramework == null)
            return t.TreeTargetFrameworkNone;
        return t.TreeTargetFrameworkDetected(FrameworkIds.MonikerOf(assembly.DetectedTargetFramework));
    }

    private static bool IsPicked(LoadedAssembly assembly, string framework)
    {
        if (!assembly.IsTargetFrameworkPicked)
            return false;
        return assembly.TargetFramework == framework;
    }

    /// <summary>Asks for the PDB <paramref name="assembly"/> reads, starting in its folder, then has it read that one.</summary>
    private static void PickPdbToRead(Ui<UiState> ui, AppState appState, LoadedAssembly assembly)
    {
        var t = ui.State.Strings;
        FileFilter[] filters = [new FileFilter(t.FileFilterPdb, ["*.pdb"]), new FileFilter(t.FileFilterAll, ["*.*"])];
        var title = t.TreeSelectPdbTitle(assembly.Name);
        ui.PickFile(title, FindFolderOf(assembly), filters, path => UsePdb(appState, assembly, path));
    }

    /// <summary>The folder the assembly's file is in; null for one read out of a package, whose path runs through it.</summary>
    private static string? FindFolderOf(LoadedAssembly assembly)
    {
        var folder = Path.GetDirectoryName(assembly.FilePath);
        if (!Directory.Exists(folder))
            return null;
        return folder;
    }
}
