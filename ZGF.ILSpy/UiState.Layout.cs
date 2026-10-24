using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

public sealed partial class UiState
{
    /// <summary>Where the views and the open documents are docked, all as tabs: side by side,
    /// stacked or in floating windows.</summary>
    public MainLayout MainLayout { get; }

    public DockLayout Layout => MainLayout.Dock;

    /// <summary>Set while the user is asked whether to reset the layout.</summary>
    public bool ConfirmingLayoutReset;

    private FocusTry _lastFocusTry = new(-1, -1);
    private DecompilationLanguage _shownLanguage;
    private DecompilerOptions _shownOptions;

    /// <summary>The saved layout with the first tab in the place of the documents, a panel the layout
    /// makes for itself opened as a new tab, and a closed panel's tab closed with it.</summary>
    private MainLayout RestoreLayout(DockLayout? saved)
    {
        var first = _appState.Tabs[0].Panel;
        var dock = DockPanels.Restore(saved, first, out var placed);
        var layout = new MainLayout(dock);
        if (!placed)
            layout.DockDocument(first, first);
        dock.NewPanel = OpenTabForPanel;
        dock.PanelClosed += OnPanelClosed;
        return layout;
    }

    private string OpenTabForPanel()
    {
        var tab = _appState.AddTab();
        AddTabView(tab);
        return tab.Panel;
    }

    /// <summary>Called when a panel is closed: clears the search with its panel, and closes the tab a
    /// closed document was, focusing the next one when it was active.</summary>
    private void OnPanelClosed(string panel)
    {
        if (panel == DockPanels.Search)
            Search.Clear();
        CloseTabOf(panel);
    }

    private void CloseTabOf(string panel)
    {
        var tab = _appState.Tabs.Find(panel);
        if (tab == null)
            return;
        DropTabView(tab);
        var wasActive = Docking.CloseTab(_appState, tab);
        if (!wasActive)
            return;
        FocusNextTab();
    }

    /// <summary>Focuses the tab of the panel the layout focuses now, else the first group's front tab.</summary>
    private void FocusNextTab()
    {
        var next = _appState.Tabs.Find(Layout.Focused);
        if (next == null)
            next = FindFrontTab();
        if (next == null)
            return;
        FocusTab(next);
    }

    /// <summary>The tab in front of the first group, docked then floating, that has one in front.
    /// Allocates nothing.</summary>
    private DocumentTab? FindFrontTab()
    {
        var docked = FindFrontTab(Layout.Root);
        if (docked != null)
            return docked;
        foreach (var window in Layout.Windows)
        {
            var floating = FindFrontTab(window.Root);
            if (floating != null)
                return floating;
        }
        return null;
    }

    private DocumentTab? FindFrontTab(DockNode node)
    {
        if (node is DockGroup group)
            return _appState.Tabs.Find(group.Active);
        if (node is not DockSplit split)
            return null;
        var first = FindFrontTab(split.First);
        if (first != null)
            return first;
        return FindFrontTab(split.Second);
    }

    /// <summary>Makes <paramref name="tab"/> the active tab and reveals what it shows in the tree, when
    /// that is listed.</summary>
    private void FocusTab(DocumentTab tab)
    {
        Docking.FocusTab(_appState, tab);
        RevealShown(tab);
    }

    private void RevealShown(DocumentTab tab)
    {
        var node = tab.Node;
        if (node == null)
            return;
        if (!_appState.IsListed(node))
            return;
        Tree.Reveal(node);
    }

    /// <summary>Focuses another tab when the active one was closed by a feature, trying again only once
    /// the tabs or the layout changed since the last try found none.</summary>
    private void FocusNextTabIfNone()
    {
        if (_appState.Tabs.Active != null)
            return;
        var tried = new FocusTry(Layout.Changes, _appState.Tabs.Count);
        if (tried == _lastFocusTry)
            return;
        _lastFocusTry = tried;
        FocusNextTab();
    }

    /// <summary>What the last try to focus another tab saw: the layout's change count and how many tabs were open.</summary>
    private readonly record struct FocusTry(int LayoutChanges, int TabCount);

    /// <summary>Docks <paramref name="tab"/> beside the panel <paramref name="beside"/> when it is not
    /// docked yet, and brings it to the front.</summary>
    private void ShowTab(DocumentTab tab, string? beside)
    {
        if (!_appState.Tabs.Contains(tab))
            return;
        AddTabView(tab);
        if (!Layout.Contains(tab.Panel))
            MainLayout.DockDocument(tab.Panel, beside);
        Layout.Activate(tab.Panel);
    }

    private void ClosePanel(string panel)
    {
        if (!Layout.Contains(panel))
            return;
        Layout.Close(panel);
    }

    /// <summary>Brings <paramref name="tab"/>'s panel to the front and makes it the active tab.</summary>
    public void ActivateTab(DocumentTab tab)
    {
        if (!_appState.Tabs.Contains(tab))
            return;
        Layout.Activate(tab.Panel);
        FocusTab(tab);
    }

    /// <summary>Called after the documents are drawn: runs the view commands the features queued meanwhile, then
    /// makes the tab the user last pressed, docked or brought to front the active one.</summary>
    public void SyncActiveTab()
    {
        RunViewCommands();
        var tab = _appState.Tabs.Find(Layout.Focused);
        if (tab == null)
            return;
        if (tab == _appState.Tabs.Active)
            return;
        FocusTab(tab);
    }

    /// <summary>Brings the next tab of the active tab's group to the front, or the previous one.</summary>
    public void CycleTab(int by)
    {
        var active = _appState.Tabs.Active;
        if (active == null)
            return;
        var group = Layout.GroupOf(active.Panel);
        if (group == null)
            return;
        var panels = group.Panels.FindAll(IsTabPanel);
        if (panels.Count < 2)
            return;
        var index = panels.IndexOf(active.Panel);
        var next = panels[WrapIndex(index + by, panels.Count)];
        var tab = _appState.Tabs.Find(next);
        if (tab == null)
            return;
        ActivateTab(tab);
    }

    private bool IsTabPanel(string panel) => _appState.Tabs.Find(panel) != null;

    private static int WrapIndex(int index, int count)
    {
        var wrapped = index % count;
        if (wrapped < 0)
            wrapped += count;
        return wrapped;
    }

    public void CloseActiveTab()
    {
        var tab = _appState.Tabs.Active;
        if (tab == null)
            return;
        Layout.Close(tab.Panel);
    }

    public void OpenAnalyzer() => MainLayout.OpenAnalyzer(_appState.Tabs.ActivePanel);

    public void OpenDecompilerOptions() => MainLayout.OpenDecompilerOptions(_appState.Tabs.ActivePanel);

    public void OpenBookmarks() => MainLayout.OpenBookmarks(_appState.Tabs.ActivePanel);

    public void OpenExports() => MainLayout.OpenExports(_appState.Tabs.ActivePanel);

    public void OpenAssemblies() => MainLayout.OpenAssemblies();

    private void OpenPanel(string panel)
    {
        switch (panel)
        {
            case DockPanels.Assemblies:
                OpenAssemblies();
                break;
            case DockPanels.Search:
                OpenSearch();
                break;
            case DockPanels.Bookmarks:
                OpenBookmarks();
                break;
            case DockPanels.Analyzer:
                OpenAnalyzer();
                break;
            case DockPanels.DecompilerOptions:
                OpenDecompilerOptions();
                break;
            case DockPanels.Exports:
                OpenExports();
                break;
        }
    }

    /// <summary>Asks the user whether to reset the layout before <see cref="ResetLayout"/> does.</summary>
    public void AskToResetLayout() => ConfirmingLayoutReset = true;

    /// <summary>Stops asking, resetting the layout when <paramref name="confirmed"/>.</summary>
    public void FinishLayoutReset(bool confirmed)
    {
        ConfirmingLayoutReset = false;
        if (confirmed)
            ResetLayout();
    }

    /// <summary>Puts the layout back as it starts out: the assemblies left of every open document,
    /// tabbed together in one group. Floating windows and the other views close; the documents stay open.</summary>
    public void ResetLayout()
    {
        var documents = MainLayout.ListDocuments();
        if (documents.Count == 0)
            documents.Add(OpenActiveTab().Panel);
        MainLayout.Reset(documents);
        ActivateActivePanel();
    }

    private DocumentTab OpenActiveTab()
    {
        var tab = _appState.AddTab();
        AddTabView(tab);
        Docking.FocusTab(_appState, tab);
        return tab;
    }

    private void ActivateActivePanel()
    {
        var panel = _appState.Tabs.ActivePanel;
        if (panel == null)
            return;
        if (!Layout.Contains(panel))
            return;
        Layout.Activate(panel);
    }

    /// <summary>Has the tabs in front of their groups decompile again once the language or the options
    /// changed; the others do when they are focused.</summary>
    private void FollowDecompilation()
    {
        if (!HasDecompilationChanged())
            return;
        _shownLanguage = _appState.Language;
        _shownOptions = _appState.DecompilerOptions;
        RefreshShownTabs();
    }

    private bool HasDecompilationChanged()
    {
        if (_appState.Language != _shownLanguage)
            return true;
        return !ReferenceEquals(_appState.DecompilerOptions, _shownOptions);
    }

    /// <summary>Makes each tab in front of its group decompile again what it shows, when the language
    /// or the options changed since.</summary>
    private void RefreshShownTabs()
    {
        foreach (var group in Layout.AllGroups())
            RefreshShownTab(group.Active);
    }

    private void RefreshShownTab(string? panel)
    {
        var tab = _appState.Tabs.Find(panel);
        if (tab == null)
            return;
        tab.EnsureCurrent(_appState.Language);
    }
}
