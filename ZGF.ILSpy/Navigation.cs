using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Decompilation;

namespace ZGF.ILSpy;

/// <summary>Moving through the code: showing nodes, following references and going back and
/// forward through the history.</summary>
public static class Navigation
{
    /// <summary>Shows <paramref name="node"/> in the preview tab, or the active tab while tabs do not
    /// preview, or else a new tab kept open, reveals it in the tree and records it in the history.</summary>
    public static void ShowNode(AppState appState, BrowseNode node, bool inNewTab = false)
    {
        if (node.Kind == NodeKind.Loading)
            return;
        appState.Intent.Begin();
        LeaveActiveTab(appState);
        ShowNodeIn(appState, ChooseDestination(appState, inNewTab), node);
        appState.History.Push(node, appState.Language);
    }

    /// <summary>Shows <paramref name="node"/>, picked in the tree, as <see cref="ShowNode"/> does, unless
    /// a tab shows it already, which is brought to the front instead.</summary>
    public static void PreviewNode(AppState appState, BrowseNode node)
    {
        var showing = FindTabShowing(appState, node);
        if (showing == null)
        {
            ShowNode(appState, node);
            return;
        }
        appState.Tabs.MakeActive(showing);
        appState.EnqueueCommand(new ViewCommand.ShowTab(showing, Beside: null));
    }

    /// <summary>The tab showing <paramref name="node"/>, the active one first; null when none does.</summary>
    private static DocumentTab? FindTabShowing(AppState appState, BrowseNode node)
    {
        var active = appState.Tabs.Active;
        if (active != null && active.Node == node)
            return active;
        foreach (var tab in appState.Tabs)
        {
            if (tab.Node == node)
                return tab;
        }
        return null;
    }

    /// <summary>Shows <paramref name="node"/>, opened from the tree, as <see cref="PreviewNode"/> does and
    /// keeps the tab showing it open; while tabs do not preview, as <see cref="ShowNode"/> does.</summary>
    public static void OpenNode(AppState appState, BrowseNode node)
    {
        if (!appState.PreviewTabs)
        {
            ShowNode(appState, node);
            return;
        }
        PreviewNode(appState, node);
        KeepActiveTabOpen(appState);
    }

    private static void KeepActiveTabOpen(AppState appState)
    {
        var active = appState.Tabs.Active;
        if (active == null)
            return;
        KeepTabOpen(appState, active);
    }

    /// <summary>Keeps <paramref name="tab"/> open, so that what is picked next shows in another tab.</summary>
    public static void KeepTabOpen(AppState appState, DocumentTab tab) => appState.Tabs.Keep(tab);

    /// <summary>Has what is picked and followed shown in the preview tab, or, when <paramref name="on"/>
    /// is false, in the active tab, the preview tab then kept open as any other.</summary>
    public static void UsePreviewTabs(AppState appState, bool on)
    {
        appState.PreviewTabs = on;
        if (on)
            return;
        var preview = appState.Tabs.Preview;
        if (preview != null)
            appState.Tabs.Keep(preview);
    }

    /// <summary>Opens the files at <paramref name="paths"/> in the background, then shows the namespace,
    /// type or member <paramref name="idString"/> names, written as XML documentation ids are, looked
    /// for in the assemblies read from them, or in every open one when there are none. Reports when
    /// none has it.</summary>
    public static async void OpenAndShowId(AppState appState, IReadOnlyList<string> paths, string idString)
    {
        var ticket = appState.Intent.Begin();
        var token = appState.Opens.Token;
        var found = await Background.Run(() => OpenAndFindId(appState, paths, idString, token));
        if (found.Error != null)
        {
            ReportOpenFailure(appState, found.Error);
            return;
        }
        if (appState.Intent.IsStale(ticket))
            return;
        if (found.Value == null)
        {
            ReportMissingId(appState, idString);
            return;
        }
        ShowNode(appState, found.Value);
    }

    private static BrowseNode? OpenAndFindId(AppState appState, IReadOnlyList<string> paths, string idString, CancellationToken token)
    {
        var opened = new List<LoadedAssembly>();
        foreach (var path in paths)
            opened.Add(appState.Assemblies.Open(path, token));
        var scope = ListIdScope(appState, opened);
        return appState.Assemblies.FindNodeById(idString, scope);
    }

    /// <summary>The assemblies an id is looked for in: those read from the files opened, a package's
    /// among them, else every open one.</summary>
    private static List<LoadedAssembly> ListIdScope(AppState appState, List<LoadedAssembly> opened)
    {
        var open = appState.Assemblies.Assemblies;
        if (opened.Count == 0)
            return open.ToList();
        return open
            .Where(a => opened.Contains(a.ListedItem))
            .ToList();
    }

    private static void ReportMissingId(AppState appState, string idString)
    {
        var t = Strings.For(appState.Locale);
        appState.ReportError(t.ErrorNavigateTo(idString));
    }

    /// <summary>Resolves <paramref name="reference"/> in the background (it may load an assembly),
    /// then shows it in the tab it was asked from, or a new one.</summary>
    public static async void Navigate(AppState appState, CodeReference reference, bool inNewTab)
    {
        if (!reference.IsNavigable)
            return;
        var ticket = appState.Intent.Begin();
        var destination = ChooseDestination(appState, inNewTab);
        appState.Intent.StartResolving(ticket);
        var found = await Background.Run(() => appState.Assemblies.FindNode(reference));
        if (appState.Intent.IsStale(ticket))
            return;
        appState.Intent.StopResolving();
        ShowResolved(appState, reference, found, destination);
    }

    private static void ShowResolved(AppState appState, CodeReference reference, Outcome<BrowseNode?> found, TabDestination destination)
    {
        var node = found.Value;
        if (node == null)
        {
            ReportUnresolvedReference(appState, reference, found.Error);
            return;
        }
        LeaveActiveTab(appState);
        ShowNodeIn(appState, destination, node);
        appState.History.Push(node, appState.Language);
    }

    /// <summary>Shows the table row a metadata token leads to where <see cref="ShowNode"/> shows a
    /// node, and records it in the history.</summary>
    public static void ShowMetadataTarget(AppState appState, MetadataTarget target, bool inNewTab)
    {
        appState.Intent.Begin();
        ShowRowIn(appState, ChooseDestination(appState, inNewTab), target);
    }

    /// <summary>Resolves <paramref name="reference"/> in the background, as <see cref="Navigate"/> does,
    /// then shows the metadata table row that defines it in the tab it was asked from, or a new one.</summary>
    public static async void ShowInMetadata(AppState appState, CodeReference reference, bool inNewTab)
    {
        if (!reference.IsNavigable)
            return;
        var ticket = appState.Intent.Begin();
        var destination = ChooseDestination(appState, inNewTab);
        appState.Intent.StartResolving(ticket);
        var found = await Background.Run(() => appState.Assemblies.FindMetadataRow(reference));
        if (appState.Intent.IsStale(ticket))
            return;
        appState.Intent.StopResolving();
        ShowResolvedRow(appState, reference, found, destination);
    }

    private static void ShowResolvedRow(AppState appState, CodeReference reference, Outcome<MetadataTarget?> found, TabDestination destination)
    {
        var target = found.Value;
        if (target == null)
        {
            ReportUnresolvedReference(appState, reference, found.Error);
            return;
        }
        ShowRowIn(appState, destination, target.Value);
    }

    private static void ShowRowIn(AppState appState, TabDestination destination, MetadataTarget target)
    {
        LeaveActiveTab(appState);
        ShowNodeIn(appState, destination, target.Node, row: target.Row);
        appState.History.Push(target.Node, appState.Language);
    }

    /// <summary>Resolves <paramref name="reference"/> in the background, then has the view open its page of
    /// the .NET API reference, or reports that it has none.</summary>
    public static async void ShowInApiReference(AppState appState, CodeReference reference)
    {
        var found = await Background.Run(() => appState.Assemblies.FindApiReferencePage(reference));
        var page = found.Value;
        if (page == null)
        {
            ReportNoApiReferencePage(appState, reference);
            return;
        }
        appState.EnqueueCommand(new ViewCommand.OpenWebPage(page));
    }

    private static void ReportNoApiReferencePage(AppState appState, CodeReference reference)
    {
        var t = Strings.For(appState.Locale);
        appState.ReportError(t.ErrorNoApiReferencePage(reference.Name), t.ErrorNoApiReferencePageDetails);
    }

    /// <summary>Goes where a node points (a base type, an assembly reference), if anywhere.</summary>
    public static void GoToTarget(AppState appState, BrowseNode node)
    {
        if (node.Target == null)
            return;
        Navigate(appState, node.Target, inNewTab: false);
    }

    public static void GoBack(AppState appState) => GoToHistoryEntry(appState, appState.History.CurrentIndex - 1);

    public static void GoForward(AppState appState) => GoToHistoryEntry(appState, appState.History.CurrentIndex + 1);

    /// <summary>Goes to the history entry at <paramref name="index"/>, however far back or forward.</summary>
    public static void GoToHistoryEntry(AppState appState, int index)
    {
        if (!CanGoTo(appState, index))
            return;
        LeaveActiveTab(appState);
        var entry = appState.History.MoveTo(index);
        if (entry == null)
            return;
        Restore(appState, entry.Value);
    }

    private static bool CanGoTo(AppState appState, int index)
    {
        if (index < 0)
            return false;
        if (index >= appState.History.Entries.Count)
            return false;
        return index != appState.History.CurrentIndex;
    }

    private static async void Restore(AppState appState, HistoryEntry entry)
    {
        var ticket = appState.Intent.Begin();
        var destination = ChooseDestination(appState, inNewTab: false);
        var place = entry.Place;
        var position = entry.Language == appState.Language ? entry.Position : null;
        var shown = FindShownNode(appState, FindTab(appState, destination), place);
        if (shown != null)
        {
            ShowNodeIn(appState, destination, shown, position);
            return;
        }
        appState.Intent.StartResolving(ticket);
        var found = await Background.Run(() => appState.Assemblies.FindNode(place));
        if (appState.Intent.IsStale(ticket))
            return;
        appState.Intent.StopResolving();
        ShowHistoryPlace(appState, place, position, found, destination);
    }

    private static BrowseNode? FindShownNode(AppState appState, DocumentTab? tab, NodePath place)
    {
        if (tab == null)
            return null;
        var node = tab.Node;
        if (node == null)
            return null;
        if (!place.Equals(tab.Location))
            return null;
        if (!appState.IsListed(node))
            return null;
        return node;
    }

    private static void ShowHistoryPlace(AppState appState, NodePath place, CodeViewPosition? position, Outcome<BrowseNode?> found,
        TabDestination destination)
    {
        var node = found.Value;
        if (node == null)
        {
            var t = Strings.For(appState.Locale);
            var details = AppState.DescribeFailure(found.Error, t.ErrorNoLongerThere(place));
            appState.ReportError(t.ErrorGoBack(place.Text), details);
            return;
        }
        ShowNodeIn(appState, destination, node, position);
    }

    internal static void ReportUnresolvedReference(AppState appState, CodeReference reference, Exception? error)
    {
        var t = Strings.For(appState.Locale);
        var details = AppState.DescribeFailure(error, t.ErrorNoDefinition(reference));
        appState.ReportError(t.ErrorResolve(reference.Name), details);
    }

    /// <summary>Records where the active tab's view is in the history entry it shows, before it shows
    /// something else.</summary>
    internal static void LeaveActiveTab(AppState appState)
    {
        var tab = appState.Tabs.Active;
        if (tab == null)
            return;
        var position = tab.Position;
        if (position == null)
            return;
        if (!IsCurrentHistoryPlace(appState, tab.Location))
            return;
        appState.History.Leave(tab.Language, position.Value);
    }

    private static bool IsCurrentHistoryPlace(AppState appState, NodePath? place)
    {
        var current = appState.History.Current;
        if (current == null)
            return false;
        return current.Value.Place.Equals(place);
    }

    /// <summary>Where what is asked for now from the active tab will show, in a new tab when
    /// <paramref name="inNewTab"/>.</summary>
    internal static TabDestination ChooseDestination(AppState appState, bool inNewTab) => new(appState.Tabs.Active, inNewTab);

    /// <summary>Shows <paramref name="node"/> in the tab <paramref name="destination"/> leads to now, makes
    /// that tab active, and commands the view to bring it to the front and reveal the node in the tree.</summary>
    internal static void ShowNodeIn(
        AppState appState,
        TabDestination destination,
        BrowseNode node,
        CodeViewPosition? position = null,
        Bookmark? target = null,
        int row = MetadataGridState.None)
    {
        var shownIn = FindOrOpenTab(appState, destination);
        appState.Tabs.MakeActive(shownIn);
        LoadIntoTab(appState, shownIn, node, new Arrival(position, target, row));
        appState.EnqueueCommand(new ViewCommand.RevealNode(node));
    }

    /// <summary>The tab <paramref name="destination"/> leads to now, brought to the front, else a new one
    /// opened; made the preview tab when it shows what tabs preview.</summary>
    private static DocumentTab FindOrOpenTab(AppState appState, TabDestination destination)
    {
        var tab = FindTab(appState, destination);
        if (tab == null)
            tab = OpenNewTab(appState);
        else
            appState.EnqueueCommand(new ViewCommand.ShowTab(tab, Beside: null));
        if (IsPreviewed(appState, destination))
            appState.Tabs.MakePreview(tab);
        return tab;
    }

    /// <summary>The open tab <paramref name="destination"/> leads to now; null when it calls for a new one.</summary>
    private static DocumentTab? FindTab(AppState appState, TabDestination destination)
    {
        if (destination.InNewTab)
            return null;
        if (appState.PreviewTabs)
            return FindPreviewTab(appState);
        return appState.Tabs.KeepIfOpen(destination.Origin);
    }

    /// <summary>The active tab while it shows nothing, as a tab just opened, else the preview tab; null
    /// when neither is.</summary>
    private static DocumentTab? FindPreviewTab(AppState appState)
    {
        var empty = FindEmptyActiveTab(appState);
        if (empty != null)
            return empty;
        return appState.Tabs.Preview;
    }

    private static DocumentTab? FindEmptyActiveTab(AppState appState)
    {
        var active = appState.Tabs.Active;
        if (active == null)
            return null;
        if (active.Content is not TabContent.Empty)
            return null;
        return active;
    }

    private static bool IsPreviewed(AppState appState, TabDestination destination)
    {
        if (destination.InNewTab)
            return false;
        return appState.PreviewTabs;
    }

    /// <summary>Opens an empty tab, which the view docks beside the active one.</summary>
    internal static DocumentTab OpenNewTab(AppState appState)
    {
        var tab = appState.AddTab();
        appState.EnqueueCommand(new ViewCommand.ShowTab(tab, appState.Tabs.Active));
        return tab;
    }
}

/// <summary>Where a node asked for shows: a new tab kept open when <see cref="InNewTab"/>; else, as decided
/// when it shows, the preview tab while tabs preview, else <see cref="Origin"/>, the tab it was asked from,
/// or a new tab when that was closed meanwhile.</summary>
internal readonly record struct TabDestination(DocumentTab? Origin, bool InNewTab);
