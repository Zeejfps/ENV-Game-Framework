using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Decompilation;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.Appearance;
using static ZGF.ILSpy.DocumentPanels;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.IconTheming;
using static ZGF.ILSpy.ScreenSettings;
using static ZGF.ILSpy.Theming;
using static ZGF.ILSpy.Trees;

namespace ZGF.ILSpy;

public sealed partial class IlSpyApp
{
    /// <summary>The screen over <paramref name="appState"/>, restored from what its settings kept, with the
    /// active tab it starts with, the tabs reopened from the open list's documents docked where they were,
    /// and any other tab in the place of the documents. It takes the theme and strings with its first frame.</summary>
    internal static UiState CreateUiState(AppState appState)
    {
        var uiState = new UiState(ReadViewSettings(appState), appState.ReportError);
        uiState.Documents.SkipPanels(appState.Lists.ListDocumentPanels());
        FollowActiveTab(uiState, appState.Tabs.Active);
        RunViewCommands(uiState, appState);
        ShowInRestoredDocuments(uiState, appState.Tabs);
        return uiState;
    }

    /// <summary>Does what comes before drawing a frame: the screen takes the theme, icons and strings the
    /// preferences pick, follows the open assemblies and analyses in the trees, what the open tabs show and
    /// the active tab; then the commands the features queued for the view run, the screen follows the tabs
    /// opened and closed in the layout and focuses another tab when none is active; then the open
    /// assemblies are read again when the options that apply as they are read changed, and the tabs in
    /// front decompile again when the language or the options changed.</summary>
    internal static void StartFrame(UiState uiState, AppState appState)
    {
        ApplyThemeAndLanguage(uiState, ResolveTheme(appState), appState.Locale);
        ApplyIconTheme(uiState, ResolveIconTheme(appState));
        SyncTrees(uiState, appState.Assemblies, appState.Analyzer, appState.ApiVisibility);
        UsePreviewTabs(appState, uiState.Display.PreviewTabs);
        FollowTabContent(uiState);
        FollowActiveTab(uiState, appState.Tabs.Active);
        RunViewCommands(uiState, appState);
        SyncTabViews(uiState, appState.Tabs);
        FocusNextTabIfNone(uiState, appState.Tabs);
        FollowLoadOptions(uiState, appState);
        FollowDecompilation(uiState, appState);
        ShowNewErrors(uiState, appState.Errors);
    }

    /// <summary>Reads the open assemblies again, as F5 does, when the options that apply as they are read changed.</summary>
    private static void FollowLoadOptions(UiState uiState, AppState appState)
    {
        if (!appState.Assemblies.UseLoadOptions(appState.DecompilerOptions))
            return;
        RefreshAssemblies(appState, uiState.Tree.ExpandedPaths());
    }

    /// <summary>Opens the error dialog on the newest error whenever one is reported.</summary>
    private static void ShowNewErrors(UiState uiState, ErrorLog errors)
    {
        uiState.ErrorDialog.ShowNewErrors(errors.Revision, errors.Entries);
    }

    private static void FollowDecompilation(UiState uiState, AppState appState)
    {
        var language = appState.Language;
        if (!uiState.ShownDecompilation.TakeChange(language, appState.DecompilerOptions))
            return;
        foreach (var tab in ListFrontTabs(uiState))
            DecompileAgainIfOutdated(appState, tab);
    }

    /// <summary>Keeps what the screen changed while it drew: the tabs keep where their views stand, and the
    /// settings keep what changed of the screen.</summary>
    internal static void KeepScreenChanges(UiState uiState, AppState appState)
    {
        KeepTabPositions(uiState, appState.Tabs);
        KeepChangedViewSettings(uiState, appState);
    }

    /// <summary>Allocates nothing.</summary>
    private static void KeepTabPositions(UiState uiState, DocumentTabs tabs)
    {
        for (var i = 0; i < tabs.Count; i++)
            KeepTabPosition(uiState, tabs[i]);
    }

    private static void KeepTabPosition(UiState uiState, DocumentTab tab)
    {
        var position = uiState.Documents.FindPosition(tab);
        if (position == null)
            return;
        tab.KeepPosition(position.Value);
    }

    private static void KeepChangedViewSettings(UiState uiState, AppState appState)
    {
        var view = TakeChangedViewSettings(uiState);
        if (view == null)
            return;
        KeepViewSettings(appState, view);
    }
}
