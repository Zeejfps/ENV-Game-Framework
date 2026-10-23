using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class AnalyzerPanelView
{
    private const string TreeId = "analyzer";
    private const string MenuId = "analyzer-menu";

    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var analyzer = w.Analyzer;
        var icons = ui.GetNodeIconPalette();
        analyzer.SetStyle(ui.Theme, icons, t);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, analyzer, t);
        ui.Separator();
        if (analyzer.Roots.Count == 0)
        {
            DocumentsView.Hint(ui, t.AnalyzerHint(Shortcuts.Analyze));
            return;
        }
        Tree(ui, w);
        ContextMenu(ui, w, t);
    }

    private static void Toolbar(Ui ui, AnalyzerTree analyzer, Strings t)
    {
        var theme = ui.Theme;
        var any = analyzer.Roots.Count > 0;
        var root = analyzer.FindSelectedRoot();
        using (ui.PaneToolbar())
        {
            if (ui.IconButton("collapse", LucideIcons.ChevronsDownUp, LucideIcons.Family, t.CodeCollapseAll, any))
                analyzer.CollapseAll();
            if (ui.IconButton("expand", LucideIcons.ChevronsUpDown, LucideIcons.Family, t.CodeExpandAll, any))
                analyzer.ExpandAll();
            ui.Separator(theme.IconSize);
            if (ui.IconButton("remove", LucideIcons.Trash2, LucideIcons.Family, t.AnalyzerRemoveAnalysis, root != null))
                analyzer.Remove(root!);
            if (ui.IconButton("clear", LucideIcons.ListX, LucideIcons.Family, t.AnalyzerRemoveAll, any))
                analyzer.Clear();
        }
    }

    private static void Tree(Ui ui, Workspace w)
    {
        var analyzer = w.Analyzer;
        var box = new Box { Padding = new Spacing(0f, ui.Theme.GapTight) };
        var result = ui.TreeView(TreeId, analyzer.Roots, analyzer, analyzer.View, box, toggleOnActivate: false);
        if (ui.HasFocus(ui.GetId(TreeId)))
            HandleKeys(ui, analyzer);
        if (result.Activated)
            GoTo(w, result.Node);
        if (result.ContextMenuRequested && result.Node is AnalyzerSymbolNode)
            ui.OpenContextMenu(MenuId, result.ContextMenuAt);
    }

    private static void HandleKeys(Ui ui, AnalyzerTree analyzer)
    {
        if (Shortcuts.AnalyzePressed(ui))
            AnalyzeSelected(analyzer);
        if (Shortcuts.DeletePressed(ui))
            RemoveSelectedAnalysis(analyzer);
    }

    private static void AnalyzeSelected(AnalyzerTree analyzer)
    {
        if (analyzer.View.Selected is AnalyzerSymbolNode picked)
            analyzer.Analyze(picked.Symbol);
    }

    private static void RemoveSelectedAnalysis(AnalyzerTree analyzer)
    {
        var root = analyzer.FindSelectedRoot();
        if (root != null)
            analyzer.Remove(root);
    }

    private static void GoTo(Workspace w, AnalyzerNode? node)
    {
        if (node is not AnalyzerSymbolNode symbol)
            return;
        var reference = symbol.Symbol.Reference;
        if (reference == null)
            return;
        w.Navigate(reference, inNewTab: false);
    }

    private static void ContextMenu(Ui ui, Workspace w, Strings t)
    {
        var analyzer = w.Analyzer;
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        if (analyzer.View.Selected is not AnalyzerSymbolNode node)
            return;
        var isRoot = analyzer.Roots.Contains(node);
        var target = node.Symbol.Reference;
        if (target != null)
            GoToItems(ui, w, t, target);
        if (!isRoot && ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
            analyzer.Analyze(node.Symbol);
        ui.MenuSeparator();
        if (isRoot && ui.MenuItem(t.AnalyzerRemove, id: "remove"))
            analyzer.Remove(node);
        if (ui.MenuItem(t.AnalyzerClear, id: "clear"))
            analyzer.Clear();
    }

    private static void GoToItems(Ui ui, Workspace w, Strings t, CodeReference target)
    {
        if (ui.MenuItem(t.AnalyzerGoTo, "Enter", id: "go-to"))
            w.Navigate(target, inNewTab: false);
        if (ui.MenuItem(t.CodeOpenInNewTab, id: "open-in-new-tab"))
            w.Navigate(target, inNewTab: true);
    }
}
