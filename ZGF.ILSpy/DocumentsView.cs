using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

internal static class DocumentsView
{
    public static void Render(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        if (w.Tabs.Count == 0)
        {
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = theme.TabHeight, Background = theme.Surface });
            using (ui.Column(new Box { Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center, Background = theme.EditorBackground }))
                ui.Text("Select a node in the tree to decompile it.", color: theme.TextMuted);
            return;
        }

        var activate = -1;
        DocumentTab? close = null;
        using (var strip = ui.TabStrip("tabs"))
        {
            for (var i = 0; i < w.Tabs.Count; i++)
            {
                var tab = w.Tabs[i];
                using (ui.Id(tab.Id))
                {
                    var title = tab.Node is { } node ? w.Tree.Label(node) : tab.Title;
                    var result = strip.Tab(title, i == w.ActiveIndex, id: "tab", tooltip: tab.Tooltip);
                    if (result.Selected) activate = i;
                    if (result.Closed) close = tab;
                }
            }
        }

        if (w.ActiveTab is { } active)
        {
            using (ui.Id(active.Id))
            {
                var result = ui.CodeView("code", active.Document, active.View);
                if (result.Navigate is { } reference)
                    w.Navigate(reference, result.NavigateInNewTab);
            }
        }

        if (activate >= 0)
            w.Activate(activate);
        if (close != null)
            w.CloseTab(close);
    }
}
