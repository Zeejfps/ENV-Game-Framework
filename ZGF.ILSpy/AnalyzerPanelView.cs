using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class AnalyzerPanelView
{
    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var theme = ui.Theme;
        var analyzer = w.Analyzer;
        analyzer.SetStyle(theme, ui.Get<NodeIconPalette>() ?? NodeIconPalette.Dark, t);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        if (analyzer.Roots.Count == 0)
        {
            using (ui.Column(new Box { Height = Sizing.Grow(), Padding = theme.ShellPaddingX, AlignX = Align.Center, AlignY = Align.Center }))
                ui.Text(t.AnalyzerHint(Shortcuts.Analyze), color: theme.TextMuted, wrap: true, align: Align.Center);
            return;
        }

        var result = ui.TreeView("analyzer", analyzer.Roots, analyzer, analyzer.View, new Box { Padding = new Spacing(0f, theme.GapTight) },
            toggleOnActivate: false);
        if (ui.HasFocus(ui.GetId("analyzer")) && Shortcuts.AnalyzePressed(ui) && analyzer.View.Selected is AnalyzerSymbolNode picked)
            analyzer.Analyze(picked.Symbol);
        if (result.Activated && result.Node is AnalyzerSymbolNode { Symbol.Reference: { } reference })
            w.Navigate(reference, inNewTab: false);
        if (result.ContextMenuRequested && result.Node is AnalyzerSymbolNode)
            ui.OpenContextMenu("analyzer-menu", result.ContextMenuAt);

        using var menu = ui.ContextMenu("analyzer-menu");
        if (!menu.IsOpen || analyzer.View.Selected is not AnalyzerSymbolNode node) return;
        if (node.Symbol.Reference is { } target)
        {
            if (ui.MenuItem(t.AnalyzerGoTo, "Enter", id: "go-to"))
                w.Navigate(target, inNewTab: false);
            if (ui.MenuItem(t.CodeOpenInNewTab, id: "open-in-new-tab"))
                w.Navigate(target, inNewTab: true);
        }
        if (!analyzer.Roots.Contains(node) && ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
            analyzer.Analyze(node.Symbol);
        ui.MenuSeparator();
        if (analyzer.Roots.Contains(node) && ui.MenuItem(t.AnalyzerRemove, id: "remove"))
            analyzer.Remove(node);
        if (ui.MenuItem(t.AnalyzerClear, id: "clear"))
            analyzer.Clear();
    }
}
