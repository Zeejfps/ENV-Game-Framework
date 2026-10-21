using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

internal static class DocumentsView
{
    public static void Render(Ui ui, Workspace w)
    {
        if (w.Tabs.Count == 0)
        {
            using (ui.Column(new Box { Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
                ui.Text("Select a node in the tree to decompile it.", color: ui.Theme.TextMuted);
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
                    var result = strip.Tab(tab.Title, i == w.ActiveIndex, id: "tab");
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
