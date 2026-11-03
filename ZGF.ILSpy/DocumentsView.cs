using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Bookmarks;
using static ZGF.ILSpy.CodeDisplay;
using static ZGF.ILSpy.Diagrams;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;

/// <summary>An open document's tab content: its omnibar over its code, grid or picture, or a hint while
/// it shows nothing.</summary>
internal static class DocumentsView
{
    public static void Render(Ui<UiState> ui, AppState appState, DocumentTab tab)
    {
        using var document = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        OmnibarView.Render(ui, appState, tab);
        Content(ui, appState, tab);
    }

    private static void Content(Ui<UiState> ui, AppState appState, DocumentTab tab)
    {
        var t = ui.State.Strings;
        switch (tab.Content)
        {
            case TabContent.Empty:
                Hint(ui, t.DocumentsHint);
                break;
            case TabContent.Picture picture:
                ResourceImageView.Render(ui, appState, tab, picture);
                break;
            case TabContent.Table table:
                RenderGrid(ui, tab, table.Grid);
                break;
            case TabContent.Diagram diagram:
                DiagramView.Render(ui, appState, tab, diagram.Types);
                break;
            case TabContent.Comparison comparison:
                ComparisonView.Render(ui, appState, tab, comparison.Assemblies);
                break;
            case TabContent.Loading loading when !NodeContentLoader.DependsOnLanguage(loading.Node):
                Hint(ui, t.MetadataLoading);
                break;
            case TabContent.Code or TabContent.Report or TabContent.Loading or TabContent.Failed or TabContent.Reloading
                or TabContent.Reopening or TabContent.Gone:
                RenderCode(ui, appState, tab);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void RenderGrid(Ui<UiState> ui, DocumentTab tab, MetadataGrid grid)
    {
        var view = ui.State.Documents.ViewOf(tab);
        var picked = ui.MetadataGridView("grid", view.GridOf(grid), ui.State.MetadataFilters);
        if (picked.Target != null && tab.Node != null)
            ui.State.Later((node: tab.Node, target: picked.Target.Value, picked.InNewTab), static (a, s) => ShowMetadataLink(a, s.node, s.target, s.InNewTab));
        if (picked.Definition != null)
            ui.State.Later((definition: picked.Definition, picked.InNewTab), static (a, s) => Navigate(a, s.definition, s.InNewTab));
    }

    /// <summary>The tab's document in the code view, with its bookmarks, or the note saying why it has none.</summary>
    public static void RenderCode(Ui<UiState> ui, AppState appState, DocumentTab tab)
    {
        var bookmarks = !tab.IsBusy && tab.Node != null && tab.Document.Error == null ? appState.Bookmarks.LinesIn(tab.Document, tab.Node) : null;
        var view = ui.State.Documents.ViewOf(tab);
        var document = FindDocumentToShow(ui.State, view, tab);
        var result = ui.CodeView("code", document, view.Code, bookmarks, tooltips: appState.Tooltips, display: ui.State.Display);
        if (result.Intent != null)
            Act(ui, tab, result.Intent);
        Zoom(ui.State, result.Zoom);
    }

    /// <summary>Does what the user asked the code view of <paramref name="tab"/> to do.</summary>
    private static void Act(Ui<UiState> ui, DocumentTab tab, CodeViewIntent intent)
    {
        var t = ui.State.Strings;
        switch (intent)
        {
            case CodeViewIntent.Navigate navigate:
                ui.State.Later(navigate, static (a, n) => Navigate(a, n.Target, n.InNewTab));
                break;
            case CodeViewIntent.ToggleBookmark toggle:
                ui.State.Later((screen: ui.State, tab, line: toggle.Line, t), static (a, s) => ToggleLineBookmark(s.screen, a, s.tab, s.line, s.t));
                break;
            case CodeViewIntent.Analyze analyze:
                ui.State.Later((screen: ui.State, entity: analyze.Entity), static (a, s) => AnalyzerPanelView.Analyze(s.screen, a, s.entity));
                break;
            case CodeViewIntent.ShowInMetadata metadata:
                ui.State.Later(metadata.Target, static (a, target) => ShowInMetadata(a, target, inNewTab: false));
                break;
            case CodeViewIntent.ShowInApiReference reference:
                ui.State.Later(reference.Entity, static (a, entity) => ShowInApiReference(a, entity));
                break;
            case CodeViewIntent.ShowInDiagram diagram:
                ui.State.Later((type: diagram.Type, title: t.DiagramTitle(diagram.Type.Name)), static (a, s) => ShowInDiagram(a, s.type, s.title));
                break;
            case CodeViewIntent.ShowInNewDiagram diagram:
                ui.State.Later((type: diagram.Type, title: t.DiagramTitle(diagram.Type.Name)), static (a, s) => ShowInNewDiagram(a, s.type, s.title));
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void Zoom(UiState uiState, CodeZoom zoom)
    {
        switch (zoom)
        {
            case CodeZoom.In:
                ZoomCode(uiState, 1);
                break;
            case CodeZoom.Out:
                ZoomCode(uiState, -1);
                break;
            case CodeZoom.Reset:
                ResetCodeZoom(uiState);
                break;
        }
    }

    private static DecompiledDocument FindDocumentToShow(UiState screen, TabView view, DocumentTab tab)
    {
        var content = tab.Content;
        if (content is TabContent.Report report)
            return view.Report.For(report.Export, screen.Strings);
        if (!IsNote(content))
            return tab.Document;
        return view.Placeholder.For(content, screen.Strings);
    }

    /// <summary>Whether <paramref name="content"/> is shown as a note saying why there is no code.</summary>
    private static bool IsNote(TabContent content) =>
        content is TabContent.Loading or TabContent.Failed or TabContent.Reloading or TabContent.Reopening or TabContent.Gone;

    /// <summary>Toggles the bookmark on <paramref name="line"/>, selecting the one made, or passing the
    /// selection of the one removed on.</summary>
    private static void ToggleLineBookmark(UiState screen, AppState appState, DocumentTab tab, int line, Strings t)
    {
        var toggle = ToggleBookmark(appState, tab, line, number => t.BookmarksDefaultName(number));
        screen.BookmarksPanel.Take(toggle, appState.Bookmarks.Items);
    }

    public static DockTab Tab(UiState screen, AppState appState, DocumentTab tab)
    {
        var title = TitleOf(screen, tab);
        var tooltip = tab.Tooltip ?? title;
        var icon = IconOf(screen, tab);
        var font = TitleFontOf(appState, tab);
        return new DockTab(title, Tooltip: tooltip, Current: tab == appState.Tabs.Active, Icon: icon, TitleFont: font);
    }

    /// <summary>Italics for the preview tab's title, the theme's font for any other.</summary>
    private static string? TitleFontOf(AppState appState, DocumentTab tab)
    {
        if (tab != appState.Tabs.Preview)
            return null;
        return TextFonts.Italic;
    }

    /// <summary>The tab's icon: its node's as the tree shows it, a diagram's, else a page.</summary>
    private static UiIcon IconOf(UiState screen, DocumentTab tab)
    {
        if (tab.Diagram != null)
            return new UiIcon(screen.Icons.TabDiagram);
        var node = tab.Node;
        if (node == null)
            return new UiIcon(screen.Icons.TabPage);
        return NodeIcons.Icon(node, screen.NodeIconPalette, screen.Icons);
    }

    /// <summary>The tab's title: its node's name as the tree labels it, a report's, else its own title, else a new tab's.</summary>
    private static string TitleOf(UiState screen, DocumentTab tab)
    {
        if (tab.Content is TabContent.Report)
            return screen.Strings.ExportReportTitle;
        var node = tab.Node;
        if (node == null)
            return tab.Title ?? screen.Strings.DocumentNewTab;
        return screen.Tree.Label(node);
    }

    /// <summary>Muted <paramref name="text"/> centered in a pane or area with nothing to show yet.</summary>
    public static void Hint(Ui<UiState> ui, string text)
    {
        var theme = ui.Theme;
        using (ui.Column(new Box { Height = Sizing.Grow(), Padding = theme.ShellPaddingX, AlignX = Align.Center, AlignY = Align.Center, Background = theme.PaneBackground }))
            ui.Text(text, color: theme.TextMuted, wrap: true, align: Align.Center);
    }
}
