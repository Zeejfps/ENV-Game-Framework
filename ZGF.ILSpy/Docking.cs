namespace ZGF.ILSpy;

/// <summary>Switching to and closing document tabs. Where their panels are docked is the screen's.</summary>
public static class Docking
{
    /// <summary>Makes <paramref name="tab"/>, when it is open, the tab the user works in, decompiling
    /// again what it shows when the language or the options changed since.</summary>
    public static void FocusTab(this Workspace w, DocumentTab tab)
    {
        if (!w.Tabs.Contains(tab))
            return;
        w.Intent.Begin();
        w.Tabs.MakeActive(tab);
        tab.EnsureCurrent(w.Language);
    }

    /// <summary>Closes <paramref name="tab"/>, cancelling what it was doing. Returns whether it was the
    /// active tab, which leaves none active until the view focuses another.</summary>
    public static bool CloseTab(this Workspace w, DocumentTab tab)
    {
        if (!w.Tabs.Contains(tab))
            return false;
        w.Intent.Begin();
        var wasActive = tab == w.Tabs.Active;
        w.Tabs.Remove(tab);
        return wasActive;
    }

    /// <summary>Closes every document tab but the active one, and empties that one; with no tab left
    /// open, opens an empty one.</summary>
    public static void CloseDocuments(this Workspace w)
    {
        var kept = w.FindTabToKeep();
        var others = w.Tabs
            .Where(t => t != kept)
            .ToList();
        foreach (var tab in others)
            w.CloseTab(tab);
        kept.Clear();
        w.Tabs.MakeActive(kept);
    }

    private static DocumentTab FindTabToKeep(this Workspace w)
    {
        var active = w.Tabs.Active;
        if (active != null)
            return active;
        if (w.Tabs.Count > 0)
            return w.Tabs[0];
        return w.OpenNewTab();
    }
}
