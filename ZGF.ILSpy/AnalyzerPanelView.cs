using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy;

internal static class AnalyzerPanelView
{
    private const string TreeId = "analyzer";
    private const string MenuId = "analyzer-menu";

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var rows = ui.State.Analyzer;
        rows.SetStyle(ui.Theme, ui.State.NodeIconPalette, ui.State.Icons, t);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, appState, rows);
        ui.Separator();
        if (rows.Roots.Count == 0)
        {
            DocumentsView.Hint(ui, t.AnalyzerHint(Shortcuts.Analyze.Label));
            return;
        }
        Tree(ui, appState);
        ContextMenu(ui, appState);
    }

    private static void Toolbar(Ui<UiState> ui, AppState appState, AnalyzerRows rows)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var analyzer = appState.Analyzer;
        var any = analyzer.Roots.Count > 0;
        var root = rows.FindSelectedRoot();
        using (ui.PaneToolbar())
        {
            if (ui.IconButton("collapse", ui.State.Icons.AnalyzerCollapseAll, t.CodeCollapseAll, any))
                rows.CollapseAll();
            if (ui.IconButton("expand", ui.State.Icons.AnalyzerExpandAll, t.CodeExpandAll, any))
                rows.ExpandAll();
            ui.Separator(theme.IconSize);
            if (ui.IconButton("remove", ui.State.Icons.AnalyzerRemove, t.AnalyzerRemoveAnalysis, root != null))
                analyzer.Remove(root!);
            if (ui.IconButton("clear", ui.State.Icons.AnalyzerRemoveAll, t.AnalyzerRemoveAll, any))
                analyzer.Clear();
        }
    }

    private static void Tree(Ui<UiState> ui, AppState appState)
    {
        var rows = ui.State.Analyzer;
        var box = new Box { Padding = new Spacing(0f, ui.Theme.GapTight) };
        var result = ui.TreeView(TreeId, rows.Roots, rows, rows.View, box, toggleOnActivate: false);
        if (ui.HasFocus(ui.GetId(TreeId)))
            HandleKeys(ui, appState, rows);
        if (result.Activated)
            GoTo(appState, result.Node);
        if (result.ContextMenuRequested && result.Node is AnalyzerSymbolNode)
            ui.OpenContextMenu(MenuId, result.ContextMenuAt);
    }

    private static void HandleKeys(Ui<UiState> ui, AppState appState, AnalyzerRows rows)
    {
        if (ui.IsPressed(Shortcuts.Analyze))
            AnalyzeSelected(appState, rows);
        if (ui.IsPressed(Shortcuts.Delete))
            RemoveSelectedAnalysis(appState, rows);
    }

    private static void AnalyzeSelected(AppState appState, AnalyzerRows rows)
    {
        if (rows.View.Selected is AnalyzerSymbolNode picked)
            Analyze(rows, appState, picked.Symbol);
    }

    private static void RemoveSelectedAnalysis(AppState appState, AnalyzerRows rows)
    {
        var root = rows.FindSelectedRoot();
        if (root != null)
            appState.Analyzer.Remove(root);
    }

    private static void GoTo(AppState appState, AnalyzerNode? node)
    {
        if (node is not AnalyzerSymbolNode symbol)
            return;
        var reference = symbol.Symbol.Reference;
        if (reference == null)
            return;
        Navigate(appState, reference, inNewTab: false);
    }

    private static void ContextMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var rows = ui.State.Analyzer;
        var analyzer = appState.Analyzer;
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        if (rows.View.Selected is not AnalyzerSymbolNode node)
            return;
        var isRoot = analyzer.Contains(node);
        var target = node.Symbol.Reference;
        if (target != null)
            GoToItems(ui, appState, target);
        if (!isRoot && ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
            Analyze(rows, appState, node.Symbol);
        ui.MenuSeparator();
        if (isRoot && ui.MenuItem(t.AnalyzerRemove, id: "remove"))
            analyzer.Remove(node);
        if (ui.MenuItem(t.AnalyzerClear, id: "clear"))
            analyzer.Clear();
    }

    private static void GoToItems(Ui<UiState> ui, AppState appState, CodeReference target)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.AnalyzerGoTo, Shortcuts.Enter, id: "go-to"))
            Navigate(appState, target, inNewTab: false);
        if (ui.MenuItem(t.CodeOpenInNewTab, id: "open-in-new-tab"))
            Navigate(appState, target, inNewTab: true);
    }

    /// <summary>Analyzes the type or member <paramref name="node"/> stands for in the analyzer, which
    /// is opened if it is not, and selects its root.</summary>
    public static void Analyze(UiState screen, AppState appState, BrowseNode node)
    {
        var root = AddToAnalyzer(appState, node);
        if (root == null)
            return;
        OpenAnalyzer(screen);
        screen.Analyzer.Show(root);
    }

    /// <summary>Analyzes <paramref name="symbol"/>, or finds the root analyzing it already, and shows that root.</summary>
    internal static void Analyze(AnalyzerRows rows, AppState appState, AnalysisSymbol symbol)
    {
        var root = AddToAnalyzer(appState, symbol);
        rows.Show(root);
    }

    /// <summary>Opens the analyzer if it is not, where a root shows as loading while what
    /// <paramref name="reference"/> points at is found and analyzed.</summary>
    public static void Analyze(UiState screen, AppState appState, CodeReference reference)
    {
        OpenAnalyzer(screen);
        AddToAnalyzer(appState, reference);
    }
}
