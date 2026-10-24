using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Moving through the code: showing nodes, following references and going back and
/// forward through the history.</summary>
public static class Navigation
{
    /// <summary>Shows <paramref name="node"/> in the active tab, or a new one, reveals it in the tree
    /// and records it in the history.</summary>
    public static void ShowNode(AppState appState, BrowseNode node, bool inNewTab = false)
    {
        if (node.Kind == NodeKind.Loading)
            return;
        appState.Intent.Begin();
        LeaveActiveTab(appState);
        var tab = inNewTab ? null : appState.Tabs.Active;
        ShowNodeIn(appState, tab, node);
        appState.History.Push(node, appState.Language);
    }

    /// <summary>Resolves <paramref name="reference"/> in the background (it may load an assembly),
    /// then shows it in the tab it was asked from, or a new one.</summary>
    public static async void Navigate(AppState appState, CodeReference reference, bool inNewTab)
    {
        if (!reference.IsNavigable)
            return;
        var ticket = appState.Intent.Begin();
        var origin = inNewTab ? null : appState.Tabs.Active;
        appState.Intent.StartResolving(ticket);
        var found = await Background.Run(() => appState.Assemblies.FindNode(reference));
        if (appState.Intent.IsStale(ticket))
            return;
        appState.Intent.StopResolving();
        ShowResolved(appState, reference, found, origin);
    }

    private static void ShowResolved(AppState appState, CodeReference reference, Outcome<BrowseNode?> found, DocumentTab? origin)
    {
        var node = found.Value;
        if (node == null)
        {
            ReportUnresolvedReference(appState, reference, found.Error);
            return;
        }
        LeaveActiveTab(appState);
        ShowNodeIn(appState, appState.Tabs.KeepIfOpen(origin), node);
        appState.History.Push(node, appState.Language);
    }

    /// <summary>Shows the table row a metadata token leads to in the active tab, or a new one, and
    /// records it in the history.</summary>
    public static void ShowMetadataTarget(AppState appState, MetadataTarget target, bool inNewTab)
    {
        appState.Intent.Begin();
        LeaveActiveTab(appState);
        var tab = inNewTab ? null : appState.Tabs.Active;
        ShowNodeIn(appState, tab, target.Node, row: target.Row);
        appState.History.Push(target.Node, appState.Language);
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
        var tab = appState.Tabs.Active;
        var place = entry.Place;
        var position = entry.Language == appState.Language ? entry.Position : null;
        var shown = FindShownNode(appState, tab, place);
        if (shown != null)
        {
            ShowNodeIn(appState, tab, shown, position);
            return;
        }
        appState.Intent.StartResolving(ticket);
        var found = await Background.Run(() => appState.Assemblies.FindNode(place));
        if (appState.Intent.IsStale(ticket))
            return;
        appState.Intent.StopResolving();
        ShowHistoryPlace(appState, place, position, found, tab);
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

    private static void ShowHistoryPlace(AppState appState, NodePath place, CodeViewPosition? position, Outcome<BrowseNode?> found, DocumentTab? tab)
    {
        var node = found.Value;
        if (node == null)
        {
            var t = Strings.For(appState.Locale);
            var details = AppState.DescribeFailure(found.Error, t.ErrorNoLongerThere(place));
            appState.ReportError(t.ErrorGoBack(place.Text), details);
            return;
        }
        ShowNodeIn(appState, appState.Tabs.KeepIfOpen(tab), node, position);
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

    /// <summary>Shows <paramref name="node"/> in <paramref name="tab"/>, or a new tab when null, makes
    /// that tab active, and commands the view to bring it to the front and reveal the node in the tree.</summary>
    internal static void ShowNodeIn(
        AppState appState,
        DocumentTab? tab,
        BrowseNode node,
        CodeViewPosition? position = null,
        Bookmark? target = null,
        int row = MetadataGridState.None)
    {
        var shownIn = ActivateOrOpenTab(appState, tab);
        appState.Tabs.MakeActive(shownIn);
        shownIn.Show(node, appState.Language, position, target, row);
        appState.EnqueueCommand(new ViewCommand.RevealNode(node));
    }

    private static DocumentTab ActivateOrOpenTab(AppState appState, DocumentTab? tab)
    {
        if (tab == null)
            return OpenNewTab(appState);
        appState.EnqueueCommand(new ViewCommand.ShowTab(tab, Beside: null));
        return tab;
    }

    /// <summary>Opens an empty tab, which the view docks beside the active one.</summary>
    internal static DocumentTab OpenNewTab(AppState appState)
    {
        var tab = appState.AddTab();
        appState.EnqueueCommand(new ViewCommand.ShowTab(tab, appState.Tabs.ActivePanel));
        return tab;
    }
}
