using static ZGF.ILSpy.Decompilation;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;

/// <summary>Opening empty document tabs, switching to them and closing them. Where their panels are
/// docked is the screen's.</summary>
public static class Docking
{
    /// <summary>Opens an empty tab for a panel the screen made for itself.</summary>
    public static DocumentTab OpenEmptyTab(AppState appState) => appState.AddTab();

    /// <summary>Makes <paramref name="tab"/>, when it is open, the tab the user works in, decompiling
    /// again what it shows when the language or the options changed since.</summary>
    public static void FocusTab(AppState appState, DocumentTab tab)
    {
        if (!appState.Tabs.Contains(tab))
            return;
        appState.Intent.Begin();
        appState.Tabs.MakeActive(tab);
        DecompileAgainIfOutdated(appState, tab);
    }

    /// <summary>Closes <paramref name="tab"/>, cancelling what it was doing. Returns whether it was the
    /// active tab, which leaves none active until the view focuses another.</summary>
    public static bool CloseTab(AppState appState, DocumentTab tab)
    {
        if (!appState.Tabs.Contains(tab))
            return false;
        appState.Intent.Begin();
        var wasActive = tab == appState.Tabs.Active;
        ForgetDiagramOf(appState, tab);
        appState.Tabs.Remove(tab);
        return wasActive;
    }

    /// <summary>Drops the diagram <paramref name="tab"/> shows, which it is about to stop showing, from
    /// the history.</summary>
    private static void ForgetDiagramOf(AppState appState, DocumentTab tab)
    {
        var diagram = tab.Diagram;
        if (diagram == null)
            return;
        appState.History.Forget(diagram);
    }

    /// <summary>Closes every document tab but the active one, and empties that one; with no tab left
    /// open, opens an empty one.</summary>
    public static void CloseDocuments(AppState appState)
    {
        var kept = FindTabToKeep(appState);
        var others = appState.Tabs
            .Where(t => t != kept)
            .ToList();
        foreach (var tab in others)
            CloseTab(appState, tab);
        ForgetDiagramOf(appState, kept);
        kept.Clear();
        appState.Tabs.MakeActive(kept);
    }

    private static DocumentTab FindTabToKeep(AppState appState)
    {
        var active = appState.Tabs.Active;
        if (active != null)
            return active;
        if (appState.Tabs.Count > 0)
            return appState.Tabs[0];
        return OpenNewTab(appState);
    }

    /// <summary>Closes every document tab, leaving none open.</summary>
    internal static void CloseAllTabs(AppState appState)
    {
        var all = appState.Tabs.ToList();
        foreach (var tab in all)
            CloseTab(appState, tab);
    }
}
