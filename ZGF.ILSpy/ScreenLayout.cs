using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.DocumentPanels;

namespace ZGF.ILSpy;

/// <summary>The layout of the screen: restoring it, opening the views in it beside the active tab, and
/// putting it back as it starts out.</summary>
public static class ScreenLayout
{
    /// <summary>The saved layout with the document panel <paramref name="first"/> in the place of the documents,
    /// a panel the layout makes for itself opened as a new tab, and a closed panel's tab closed with it.</summary>
    internal static MainLayout RestoreLayout(UiState uiState, DockLayout? saved, string first)
    {
        var dock = DockPanels.Restore(saved, first, out var placed);
        var layout = new MainLayout(dock);
        if (!placed)
            layout.DockDocument(first, first);
        dock.NewPanel = () => OpenTabForPanel(uiState);
        dock.PanelClosed += panel => OnPanelClosed(uiState, panel);
        return layout;
    }

    public static void OpenAnalyzer(UiState uiState) => uiState.MainLayout.OpenAnalyzer(FindActivePanel(uiState));

    public static void OpenDecompilerOptions(UiState uiState) => uiState.MainLayout.OpenDecompilerOptions(FindActivePanel(uiState));

    public static void OpenBookmarks(UiState uiState) => uiState.MainLayout.OpenBookmarks(FindActivePanel(uiState));

    public static void OpenExports(UiState uiState) => uiState.MainLayout.OpenExports(FindActivePanel(uiState));

    public static void OpenAssembliesPanel(UiState uiState) => uiState.MainLayout.OpenAssemblies();

    /// <summary>Docks the view <paramref name="panel"/>, one of <see cref="DockPanels"/>, if it is not, and brings it to the front.</summary>
    public static void OpenPanel(UiState uiState, string panel)
    {
        switch (panel)
        {
            case DockPanels.Assemblies:
                OpenAssembliesPanel(uiState);
                break;
            case DockPanels.Search:
                OpenSearch(uiState);
                break;
            case DockPanels.Bookmarks:
                OpenBookmarks(uiState);
                break;
            case DockPanels.Analyzer:
                OpenAnalyzer(uiState);
                break;
            case DockPanels.DecompilerOptions:
                OpenDecompilerOptions(uiState);
                break;
            case DockPanels.Exports:
                OpenExports(uiState);
                break;
        }
    }

    /// <summary>Docks the search panel under the documents if it is closed, brings it to the front
    /// and puts the caret in its search box.</summary>
    public static void OpenSearch(UiState uiState)
    {
        uiState.MainLayout.OpenSearch(FindActivePanel(uiState));
        uiState.IsSearchFocusPending = true;
    }

    /// <summary>Opens the search panel with its query kept to <paramref name="node"/>'s assembly, or to
    /// the namespace it is.</summary>
    public static void ScopeSearch(UiState uiState, BrowseNode node)
    {
        uiState.Search.ScopeTo(node);
        OpenSearch(uiState);
    }

    /// <summary>True once after <see cref="OpenSearch"/>: the search box should take focus.</summary>
    public static bool TakeSearchFocus(UiState uiState)
    {
        var focus = uiState.IsSearchFocusPending;
        uiState.IsSearchFocusPending = false;
        return focus;
    }

    /// <summary>Asks the user whether to reset the layout before <see cref="ResetLayout"/> does.</summary>
    public static void AskToResetLayout(UiState uiState)
    {
        uiState.ConfirmingLayoutReset = true;
    }

    /// <summary>Stops asking, resetting the layout when <paramref name="confirmed"/>.</summary>
    public static void FinishLayoutReset(UiState uiState, bool confirmed)
    {
        uiState.ConfirmingLayoutReset = false;
        if (confirmed)
            ResetLayout(uiState);
    }

    /// <summary>Puts the layout back as it starts out: the assemblies left of every open document,
    /// tabbed together in one group. Floating windows and the other views close; the documents stay open.</summary>
    public static void ResetLayout(UiState uiState)
    {
        var documents = uiState.MainLayout.ListDocuments();
        if (documents.Count == 0)
            documents.Add(OpenActiveTabPanel(uiState));
        uiState.MainLayout.Reset(documents);
        ActivateActivePanel(uiState);
    }

    /// <summary>A new document panel, with an empty tab asked for to be opened in it as the active tab.</summary>
    private static string OpenActiveTabPanel(UiState uiState)
    {
        var panel = uiState.Documents.NewPanel();
        uiState.AppCommands.Enqueue(new AppCommand.OpenTab(panel, Focus: true));
        return panel;
    }

    private static void ActivateActivePanel(UiState uiState)
    {
        var panel = FindActivePanel(uiState);
        if (panel == null)
            return;
        if (!uiState.Layout.Contains(panel))
            return;
        uiState.Layout.Activate(panel);
    }
}
