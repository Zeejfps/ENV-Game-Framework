using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>An open document's tab content: its code, grid or picture, or a hint while it shows
/// nothing.</summary>
internal static class DocumentsView
{
    public static void Render(Ui<UiState> ui, Workspace w, DocumentTab tab)
    {
        var t = ui.State.Strings;
        if (IsEmpty(tab))
        {
            Hint(ui, t.DocumentsHint);
            return;
        }
        if (tab.Image is { } image)
        {
            ResourceImageView.Render(ui, w, tab, image);
            return;
        }
        if (tab.Grid is { } grid)
        {
            var view = ui.State.ViewOf(tab);
            var picked = ui.MetadataGridView("grid", grid, view.Grid, w.MetadataFilters);
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
        RenderCode(ui, w, tab);
    }

    /// <summary>The tab's document in the code view, with its bookmarks.</summary>
    public static void RenderCode(Ui<UiState> ui, Workspace w, DocumentTab tab)
    {
        var bookmarks = tab is { IsBusy: false, Node: { } node, Document.IsError: false } ? w.Bookmarks.LinesIn(tab.Document, node.Path) : null;
        var view = ui.State.ViewOf(tab);
        var result = ui.CodeView("code", tab.Document, view.Code, bookmarks, tooltips: w.Tooltips);
        if (result.Navigate is { } reference)
            w.Navigate(reference, result.NavigateInNewTab);
        if (result.ToggleBookmark is { } line)
            w.ToggleBookmark(tab, line);
        if (result.Analyze is { } analyzed)
            w.Analyze(analyzed);
    }

    private static bool IsEmpty(DocumentTab tab)
    {
        if (tab.ShowsText)
            return false;
        return tab.Location == null;
    }

    public static DockTab Tab(Workspace w, DocumentTab tab) =>
        new(tab.Node is { } node ? w.Tree.Label(node) : tab.Title, Tooltip: tab.Tooltip, Current: tab == w.Tabs.Active);

    /// <summary>Muted <paramref name="text"/> centered in a pane or area with nothing to show yet.</summary>
    public static void Hint(Ui<UiState> ui, string text)
    {
        var theme = ui.Theme;
        using (ui.Column(new Box { Height = Sizing.Grow(), Padding = theme.ShellPaddingX, AlignX = Align.Center, AlignY = Align.Center, Background = theme.PaneBackground }))
            ui.Text(text, color: theme.TextMuted, wrap: true, align: Align.Center);
    }
}
