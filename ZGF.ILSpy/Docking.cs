namespace ZGF.ILSpy;

/// <summary>Opening the views, switching and closing document tabs, and resetting the layout.</summary>
public static class Docking
{
    public static void OpenAnalyzer(this Workspace w) => w.MainLayout.OpenAnalyzer(w.Tabs.ActivePanel);

    public static void OpenDecompilerOptions(this Workspace w) => w.MainLayout.OpenDecompilerOptions(w.Tabs.ActivePanel);

    public static void OpenBookmarks(this Workspace w) => w.MainLayout.OpenBookmarks(w.Tabs.ActivePanel);

    public static void OpenAssemblies(this Workspace w) => w.MainLayout.OpenAssemblies();

    public static void Activate(this Workspace w, DocumentTab tab)
    {
        if (!w.Tabs.Contains(tab))
            return;
        w.Layout.Activate(tab.Panel);
        w.Focus(tab);
    }

    /// <summary>Called after the documents are recorded: makes the tab the user last pressed, docked
    /// or brought to front the active one.</summary>
    public static void SyncActiveTab(this Workspace w)
    {
        var tab = w.Tabs.Find(w.Layout.Focused);
        if (tab == null)
            return;
        if (tab == w.Tabs.Active)
            return;
        w.Focus(tab);
    }

    /// <summary>Brings the next tab of the active tab's group to the front, or the previous one.</summary>
    public static void CycleTab(this Workspace w, int by)
    {
        var active = w.Tabs.Active;
        if (active == null)
            return;
        var group = w.Layout.GroupOf(active.Panel);
        if (group == null)
            return;
        var panels = group.Panels.FindAll(w.IsTabPanel);
        if (panels.Count < 2)
            return;
        var index = panels.IndexOf(active.Panel);
        var next = panels[WrapIndex(index + by, panels.Count)];
        var tab = w.Tabs.Find(next);
        if (tab == null)
            return;
        w.Activate(tab);
    }

    private static bool IsTabPanel(this Workspace w, string panel) => w.Tabs.Find(panel) != null;

    private static int WrapIndex(int index, int count)
    {
        var wrapped = index % count;
        if (wrapped < 0)
            wrapped += count;
        return wrapped;
    }

    public static void CloseTab(this Workspace w, DocumentTab tab) => w.Layout.Close(tab.Panel);

    /// <summary>Called when a panel is closed: clears the search with its panel, and forgets the tab
    /// a closed document was, focusing the next one when it was active.</summary>
    internal static void OnPanelClosed(this Workspace w, string panel)
    {
        if (panel == DockPanels.Search)
            w.Search.Clear();
        w.RemoveTab(panel);
    }

    private static void RemoveTab(this Workspace w, string panel)
    {
        var tab = w.Tabs.Find(panel);
        if (tab == null)
            return;
        w.Intent.Begin();
        var wasActive = tab == w.Tabs.Active;
        w.Tabs.Remove(tab);
        if (!wasActive)
            return;
        w.FocusNextTab();
    }

    private static void FocusNextTab(this Workspace w)
    {
        var next = w.Tabs.Find(w.Layout.Focused);
        if (next == null)
            next = w.FindFrontTab();
        if (next == null)
            return;
        w.Focus(next);
    }

    private static DocumentTab? FindFrontTab(this Workspace w)
    {
        foreach (var group in w.Layout.AllGroups())
        {
            var tab = w.Tabs.Find(group.Active);
            if (tab != null)
                return tab;
        }
        return null;
    }

    private static void Focus(this Workspace w, DocumentTab tab)
    {
        w.Intent.Begin();
        w.Tabs.MakeActive(tab);
        tab.EnsureCurrent(w.Language);
        var node = tab.Node;
        if (node != null && w.IsListed(node))
            w.Tree.Reveal(node);
    }

    /// <summary>Asks the user whether to reset the layout before <see cref="ResetLayout"/> does.</summary>
    public static void AskToResetLayout(this Workspace w) => w.ConfirmingLayoutReset = true;

    /// <summary>Stops asking, resetting the layout when <paramref name="confirmed"/>.</summary>
    public static void FinishLayoutReset(this Workspace w, bool confirmed)
    {
        w.ConfirmingLayoutReset = false;
        if (confirmed)
            w.ResetLayout();
    }

    /// <summary>Puts the layout back as it starts out: the assemblies left of every open document,
    /// tabbed together in one group. Floating windows and the other views close; the documents stay open.</summary>
    public static void ResetLayout(this Workspace w)
    {
        var documents = w.MainLayout.ListDocuments();
        if (documents.Count == 0)
            documents.Add(w.AddActiveTab().Panel);
        w.MainLayout.Reset(documents);
        w.ActivateActivePanel();
    }

    private static DocumentTab AddActiveTab(this Workspace w)
    {
        var tab = w.AddTab();
        w.Tabs.MakeActive(tab);
        return tab;
    }

    private static void ActivateActivePanel(this Workspace w)
    {
        var panel = w.Tabs.ActivePanel;
        if (panel == null)
            return;
        if (!w.Layout.Contains(panel))
            return;
        w.Layout.Activate(panel);
    }
}
