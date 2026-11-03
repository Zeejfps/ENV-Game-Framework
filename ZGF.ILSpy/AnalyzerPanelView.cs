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
        Tree(ui);
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
            if (ui.IconButton("remove", ui.State.Icons.AnalyzerRemove, t.AnalyzerRemoveAnalysis, root != null) && root != null)
                ui.State.Later(root, static (a, r) => a.Analyzer.Remove(r));
            if (ui.IconButton("clear", ui.State.Icons.AnalyzerRemoveAll, t.AnalyzerRemoveAll, any))
                ui.State.Later(static a => a.Analyzer.Clear());
        }
    }

    private static void Tree(Ui<UiState> ui)
    {
        var rows = ui.State.Analyzer;
        var box = new Box { Padding = new Spacing(0f, ui.Theme.GapTight) };
        var result = ui.TreeView(TreeId, rows.Roots, rows, rows.View, box, toggleOnActivate: false);
        if (ui.HasFocus(ui.GetId(TreeId)))
            HandleKeys(ui, rows);
        if (result.Activated)
            ui.State.Later(result.Node, static (a, n) => GoTo(a, n));
        if (result.ContextMenuRequested && result.Node is AnalyzerSymbolNode)
            ui.OpenContextMenu(MenuId, result.ContextMenuAt);
    }

    private static void HandleKeys(Ui<UiState> ui, AnalyzerRows rows)
    {
        if (ui.IsPressed(Shortcuts.Analyze))
            AnalyzeSelected(ui.State, rows);
        if (ui.IsPressed(Shortcuts.Delete))
            RemoveSelectedAnalysis(ui.State, rows);
    }

    private static void AnalyzeSelected(UiState screen, AnalyzerRows rows)
    {
        if (rows.View.Selected is AnalyzerSymbolNode picked)
            screen.Later((rows, symbol: picked.Symbol), static (a, s) => Analyze(s.rows, a, s.symbol));
    }

    private static void RemoveSelectedAnalysis(UiState screen, AnalyzerRows rows)
    {
        var root = rows.FindSelectedRoot();
        if (root != null)
            screen.Later(root, static (a, r) => a.Analyzer.Remove(r));
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
            GoToItems(ui, target);
        if (!isRoot && ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
            ui.State.Later((rows, symbol: node.Symbol), static (a, s) => Analyze(s.rows, a, s.symbol));
        ui.MenuSeparator();
        if (isRoot && ui.MenuItem(t.AnalyzerRemove, id: "remove"))
            ui.State.Later(node, static (a, n) => a.Analyzer.Remove(n));
        if (ui.MenuItem(t.AnalyzerClear, id: "clear"))
            ui.State.Later(static a => a.Analyzer.Clear());
    }

    private static void GoToItems(Ui<UiState> ui, CodeReference.Navigable target)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.AnalyzerGoTo, Shortcuts.Enter, id: "go-to"))
            ui.State.Later(target, static (a, r) => Navigate(a, r, inNewTab: false));
        if (ui.MenuItem(t.CodeOpenInNewTab, id: "open-in-new-tab"))
            ui.State.Later(target, static (a, r) => Navigate(a, r, inNewTab: true));
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
    public static void Analyze(UiState screen, AppState appState, CodeReference.Navigable reference)
    {
        OpenAnalyzer(screen);
        AddToAnalyzer(appState, reference);
    }
}
