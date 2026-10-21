using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class DocumentsView
{
    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var theme = ui.Theme;
        if (w.Tabs.Count == 0)
        {
            using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = theme.TabHeight, Background = theme.Surface });
            Hint(ui, t);
            return;
        }

        using (var dock = ui.DockSpace("documents", w.Documents, w, static (w, panel) => Tab(w, panel)))
        {
            while (dock.Next(out var panel))
            {
                if (w.FindTab(panel) is not { } tab) continue;
                if (tab.Location == null)
                {
                    Hint(ui, t);
                    continue;
                }
                var bookmarks = tab is { IsBusy: false, Node: { } node, Document.IsError: false } ? w.Bookmarks.LinesIn(tab.Document, node.Path) : null;
                var result = ui.CodeView("code", tab.Document, tab.View, t, bookmarks);
                if (result.Navigate is { } reference)
                    w.Navigate(reference, result.NavigateInNewTab);
                if (result.ToggleBookmark is { } line)
                    w.ToggleBookmark(tab, line);
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

    private static void Hint(Ui ui, Strings t)
    {
        var theme = ui.Theme;
        using (ui.Column(new Box { Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center, Background = theme.EditorBackground }))
            ui.Text(t.DocumentsHint, color: theme.TextMuted);
    }
}
