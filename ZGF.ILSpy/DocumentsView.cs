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
        if (tab.Image is { } image)
        {
            ResourceImageView.Render(ui, w, t, tab, image);
            return;
        }
        if (tab.Grid is { } grid)
        {
            var picked = ui.MetadataGridView("grid", grid, tab.GridView, w.MetadataFilters, t);
            if (picked.Target is { } target)
                w.Show(target, picked.InNewTab);
            if (picked.Definition is { } definition)
                w.Navigate(definition, picked.InNewTab);
            return;
        }
        if (tab is { IsBusy: true, Node.DependsOnLanguage: false })
        {
            Hint(ui, t.MetadataLoading);
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
        new(tab.Node is { } node ? w.Tree.Label(node) : tab.Title, Tooltip: tab.Tooltip, Current: tab == w.ActiveTab);

    /// <summary>Muted <paramref name="text"/> centered in a pane or area with nothing to show yet.</summary>
    public static void Hint(Ui ui, string text)
    {
        var theme = ui.Theme;
        using (ui.Column(new Box { Height = Sizing.Grow(), Padding = theme.ShellPaddingX, AlignX = Align.Center, AlignY = Align.Center, Background = theme.PaneBackground }))
            ui.Text(text, color: theme.TextMuted, wrap: true, align: Align.Center);
    }
}
