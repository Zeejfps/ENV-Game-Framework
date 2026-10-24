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
        var first = PanelOf(_appState.Tabs[0]);
        var dock = DockPanels.Restore(saved, first, out var placed);
        var layout = new MainLayout(dock);
        if (!placed)
            layout.DockDocument(first, first);
        dock.NewPanel = OpenTabForPanel;
        dock.PanelClosed += OnPanelClosed;
        return layout;
    }

    /// <summary>What the screen asked of the app state while it drew, for the app to run once the frame is drawn.</summary>
    public Queue<AppCommand> AppCommands { get; } = new();

    private string OpenTabForPanel()
    {
        var panel = NewDocumentPanel();
        AppCommands.Enqueue(new AppCommand.OpenTab(panel, Focus: false));
        return panel;
    }

    /// <summary>Called when a panel is closed: clears the search with its panel, and asks for the tab a
    /// closed document was to be closed.</summary>
    private void OnPanelClosed(string panel)
    {
        if (panel == DockPanels.Search)
            Search.Clear();
        CloseTabOf(panel);
    }

    private void CloseTabOf(string panel)
    {
        var tab = FindTab(panel);
        if (tab == null)
            return;
        DropTabView(tab);
        AppCommands.Enqueue(new AppCommand.CloseTab(tab));
    }

    /// <summary>Focuses the tab of the panel the layout focuses now, else the first group's front tab.</summary>
    public void FocusNextTab()
    {
        var next = FindTab(Layout.Focused);
        if (next == null)
            next = FindFrontTab();
        if (next == null)
            return;
        AskToFocusTab(next);
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
            return FindTab(group.Active);
        if (node is not DockSplit split)
            return null;
        var first = FindFrontTab(split.First);
        if (first != null)
            return first;
        return FindFrontTab(split.Second);
    }

    /// <summary>Asks for <paramref name="tab"/> to become the active tab, with what it shows revealed in the tree.</summary>
    private void AskToFocusTab(DocumentTab tab)
    {
        AppCommands.Enqueue(new AppCommand.FocusTab(tab));
    }

    /// <summary>Focuses another tab when the active one was closed by a feature, trying again only once
    /// the tabs or the layout changed since the last try found none.</summary>
    public void FocusNextTabIfNone(DocumentTabs tabs)
    {
        if (tabs.Active != null)
            return;
        var tried = new FocusTry(Layout.Changes, tabs.Count);
        if (tried == _lastFocusTry)
            return;
        _lastFocusTry = tried;
        FocusNextTab();
    }

    /// <summary>What the last try to focus another tab saw: the layout's change count and how many tabs were open.</summary>
    private readonly record struct FocusTry(int LayoutChanges, int TabCount);

    /// <summary>Docks <paramref name="tab"/>, an open tab, beside the tab <paramref name="beside"/> when it
    /// is not docked yet, and brings it to the front.</summary>
    public void ShowTab(DocumentTab tab, DocumentTab? beside)
    {
        AddTabView(tab);
        var panel = PanelOf(tab);
        if (!Layout.Contains(panel))
            MainLayout.DockDocument(panel, FindPanel(beside));
        Layout.Activate(panel);
    }

    private void ClosePanel(string panel)
    {
        if (!Layout.Contains(panel))
            return;
        Layout.Close(panel);
    }

    /// <summary>Brings <paramref name="tab"/>'s panel to the front and asks for it to become the active tab.</summary>
    public void ActivateTab(DocumentTab tab)
    {
        if (!_tabViews.ContainsKey(tab))
            return;
        Layout.Activate(PanelOf(tab));
        AskToFocusTab(tab);
    }

    /// <summary>Called after the documents are drawn: asks for the tab the user last pressed, docked or
    /// brought to front to become the active one, unless it is <paramref name="active"/>.</summary>
    public void SyncActiveTab(DocumentTab? active)
    {
        FollowActiveTab(active);
        var tab = FindTab(Layout.Focused);
        if (tab == null)
            return;
        if (tab == active)
            return;
        AskToFocusTab(tab);
    }

    /// <summary>Brings the next tab of the active tab's group to the front, or the previous one.</summary>
    public void CycleTab(int by)
    {
        var active = _activeTab;
        if (active == null)
            return;
        var activePanel = PanelOf(active);
        var group = Layout.GroupOf(activePanel);
        if (group == null)
            return;
        var panels = group.Panels.FindAll(IsTabPanel);
        if (panels.Count < 2)
            return;
        var index = panels.IndexOf(activePanel);
        var next = panels[WrapIndex(index + by, panels.Count)];
        var tab = FindTab(next);
        if (tab == null)
            return;
        ActivateTab(tab);
    }

    private bool IsTabPanel(string panel) => FindTab(panel) != null;

    private static int WrapIndex(int index, int count)
    {
        var wrapped = index % count;
        if (wrapped < 0)
            wrapped += count;
        return wrapped;
    }

    public void CloseActiveTab()
    {
        var tab = _activeTab;
        if (tab == null)
            return;
        Layout.Close(PanelOf(tab));
    }

    public void OpenAnalyzer() => MainLayout.OpenAnalyzer(FindActivePanel());

    public void OpenDecompilerOptions() => MainLayout.OpenDecompilerOptions(FindActivePanel());

    public void OpenBookmarks() => MainLayout.OpenBookmarks(FindActivePanel());

    public void OpenExports() => MainLayout.OpenExports(FindActivePanel());

    private string? FindActivePanel() => FindPanel(_activeTab);

    public void OpenAssemblies() => MainLayout.OpenAssemblies();

    /// <summary>Docks the view <paramref name="panel"/>, one of <see cref="DockPanels"/>, if it is not, and brings it to the front.</summary>
    public void OpenPanel(string panel)
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
            documents.Add(OpenActiveTabPanel());
        MainLayout.Reset(documents);
        ActivateActivePanel();
    }

    /// <summary>A new document panel, with an empty tab asked for to be opened in it as the active tab.</summary>
    private string OpenActiveTabPanel()
    {
        var panel = NewDocumentPanel();
        AppCommands.Enqueue(new AppCommand.OpenTab(panel, Focus: true));
        return panel;
    }

    private void ActivateActivePanel()
    {
        var panel = FindActivePanel();
        if (panel == null)
            return;
        if (!Layout.Contains(panel))
            return;
        Layout.Activate(panel);
    }

    /// <summary>Whether <paramref name="language"/> or <paramref name="options"/> differ from what the tabs in
    /// front were last brought up to date with; records them as what they are brought up to date with now.</summary>
    public bool TakeDecompilationChange(DecompilationLanguage language, DecompilerOptions options)
    {
        if (!HasDecompilationChanged(language, options))
            return false;
        _shownLanguage = language;
        _shownOptions = options;
        return true;
    }

    private bool HasDecompilationChanged(DecompilationLanguage language, DecompilerOptions options)
    {
        if (language != _shownLanguage)
            return true;
        return !ReferenceEquals(options, _shownOptions);
    }

    /// <summary>The tab in front of each group that has one.</summary>
    public List<DocumentTab> ListFrontTabs()
    {
        var tabs = new List<DocumentTab>();
        foreach (var group in Layout.AllGroups())
            AddFrontTab(tabs, group.Active);
        return tabs;
    }

    private void AddFrontTab(List<DocumentTab> tabs, string? panel)
    {
        var tab = FindTab(panel);
        if (tab == null)
            return;
        tabs.Add(tab);
    }
}
