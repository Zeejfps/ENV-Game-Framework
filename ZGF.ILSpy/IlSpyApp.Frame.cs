using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy;

public sealed partial class IlSpyApp
{
    /// <summary>The screen over <paramref name="appState"/>, restored from what its settings kept, with its
    /// first tab in the place of the documents and the active tab it starts with. It takes the theme and
    /// strings with its first frame.</summary>
    internal static UiState CreateUiState(AppState appState)
    {
        var uiState = new UiState(ReadViewSettings(appState), appState.ReportError);
        uiState.ShowInRestoredDocuments(appState.Tabs[0]);
        uiState.FollowActiveTab(appState.Tabs.Active);
        return uiState;
    }

    /// <summary>Does what comes before drawing a frame: the screen takes the theme and strings the
    /// preferences pick, follows the open assemblies and analyses in the trees, the open tabs in the
    /// layout and the active tab, and focuses another tab when none is active; then the commands the
    /// features queued for the view run, and the tabs in front decompile again when the language or the
    /// options changed.</summary>
    internal static void StartFrame(UiState uiState, AppState appState)
    {
        uiState.ApplyThemeAndLanguage(appState.Theme, appState.Locale);
        uiState.SyncTrees(appState.Assemblies, appState.Analyzer, appState.ShowNonPublic);
        uiState.SyncTabViews(appState.Tabs);
        uiState.FollowActiveTab(appState.Tabs.Active);
        uiState.FocusNextTabIfNone(appState.Tabs);
        RunViewCommands(uiState, appState);
        FollowDecompilation(uiState, appState);
    }

    private static void FollowDecompilation(UiState uiState, AppState appState)
    {
        var language = appState.Language;
        if (!uiState.TakeDecompilationChange(language, appState.DecompilerOptions))
            return;
        foreach (var tab in uiState.ListFrontTabs())
            tab.EnsureCurrent(language);
    }

    /// <summary>Does what comes after drawing a frame: the tabs keep where their views stand, and the
    /// settings keep what changed of the screen.</summary>
    internal static void FinishFrame(UiState uiState, AppState appState)
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
        var position = uiState.FindViewPosition(tab);
        if (position == null)
            return;
        tab.KeepPosition(position.Value);
    }

    private static void KeepChangedViewSettings(UiState uiState, AppState appState)
    {
        var view = uiState.TakeChangedViewSettings();
        if (view == null)
            return;
        KeepViewSettings(appState, view);
    }
}
