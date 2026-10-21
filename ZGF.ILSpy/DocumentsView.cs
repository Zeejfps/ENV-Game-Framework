using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

internal static class DocumentsView
{
    public static void Render(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        if (w.Tabs.Count == 0)
        {
            using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = theme.TabHeight, Background = theme.Surface });
            Hint(ui);
            return;
        }

        using (var dock = ui.DockSpace("documents", w.Documents, w, static (w, panel) => Tab(w, panel)))
        {
            while (dock.Next(out var panel))
            {
                if (w.FindTab(panel) is not { } tab) continue;
                if (tab.Location == null)
                {
                    Hint(ui);
                    continue;
                }
                var result = ui.CodeView("code", tab.Document, tab.View);
                if (result.Navigate is { } reference)
                    w.Navigate(reference, result.NavigateInNewTab);
            }
        }
        w.SyncActiveTab();
    }

    private static DockTab Tab(Workspace w, string panel)
    {
        if (w.FindTab(panel) is not { } tab)
            return new DockTab(panel);
        return new DockTab(tab.Node is { } node ? w.Tree.Label(node) : tab.Title, Tooltip: tab.Tooltip);
    }

    private static void Hint(Ui ui)
    {
        var theme = ui.Theme;
        using (ui.Column(new Box { Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center, Background = theme.EditorBackground }))
            ui.Text("Select a node in the tree to decompile it.", color: theme.TextMuted);
    }
}
