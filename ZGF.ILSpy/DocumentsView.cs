using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>An open document's tab content: its code, or a hint while it shows nothing.</summary>
internal static class DocumentsView
{
    public static void Render(Ui ui, Workspace w, Strings t, DocumentTab tab)
    {
        if (tab.Location == null)
        {
            Hint(ui, t.DocumentsHint);
            return;
        }
        var bookmarks = tab is { IsBusy: false, Node: { } node, Document.IsError: false } ? w.Bookmarks.LinesIn(tab.Document, node.Path) : null;
        var result = ui.CodeView("code", tab.Document, tab.View, t, bookmarks);
        if (result.Navigate is { } reference)
            w.Navigate(reference, result.NavigateInNewTab);
        if (result.ToggleBookmark is { } line)
            w.ToggleBookmark(tab, line);
        if (result.Analyze is { } analyzed)
            w.Analyze(analyzed);
    }

    public static DockTab Tab(Workspace w, DocumentTab tab) =>
        new(tab.Node is { } node ? w.Tree.Label(node) : tab.Title, Tooltip: tab.Tooltip);

    public static void Hint(Ui ui, string text)
    {
        var theme = ui.Theme;
        using (ui.Column(new Box { Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center, Background = theme.EditorBackground }))
            ui.Text(text, color: theme.TextMuted);
    }
}
