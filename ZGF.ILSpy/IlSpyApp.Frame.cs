using static ZGF.ILSpy.Decompilation;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.Appearance;
using static ZGF.ILSpy.DocumentPanels;
using static ZGF.ILSpy.IconTheming;
using static ZGF.ILSpy.ScreenSettings;
using static ZGF.ILSpy.Theming;
using static ZGF.ILSpy.Trees;

namespace ZGF.ILSpy;

public sealed partial class IlSpyApp
{
    /// <summary>The screen over <paramref name="appState"/>, restored from what its settings kept, with its
    /// first tab in the place of the documents and the active tab it starts with. It takes the theme and
    /// strings with its first frame.</summary>
    internal static UiState CreateUiState(AppState appState)
    {
        var uiState = new UiState(ReadViewSettings(appState), appState.ReportError);
        ShowInRestoredDocuments(uiState, appState.Tabs[0]);
        FollowActiveTab(uiState, appState.Tabs.Active);
        return uiState;
    }

    /// <summary>Does what comes before drawing a frame: the screen takes the theme, icons and strings the
    /// preferences pick, follows the open assemblies and analyses in the trees, the open tabs in the
    /// layout and the active tab, and focuses another tab when none is active; then the commands the
    /// features queued for the view run, and the tabs in front decompile again when the language or the
    /// options changed.</summary>
    internal static void StartFrame(UiState uiState, AppState appState)
    {
        ApplyThemeAndLanguage(uiState, ResolveTheme(appState), appState.Locale);
        ApplyIconTheme(uiState, ResolveIconTheme(appState));
        SyncTrees(uiState, appState.Assemblies, appState.Analyzer, appState.ApiVisibility);
        SyncTabViews(uiState, appState.Tabs);
        FollowActiveTab(uiState, appState.Tabs.Active);
        FocusNextTabIfNone(uiState, appState.Tabs);
        RunViewCommands(uiState, appState);
        FollowDecompilation(uiState, appState);
        ShowNewErrors(uiState, appState.Errors);
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
