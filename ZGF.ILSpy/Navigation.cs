using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>Moving through the code: showing nodes, following references and going back and
/// forward through the history.</summary>
public static class Navigation
{
    /// <summary>Shows <paramref name="node"/> in the active tab, or a new one, reveals it in the tree
    /// and records it in the history.</summary>
    public static void Show(this Workspace w, BrowseNode node, bool inNewTab = false)
    {
        if (node.Kind == NodeKind.Loading)
            return;
        w.Intent.Begin();
        w.LeaveActive();
        var tab = inNewTab ? null : w.Tabs.Active;
        w.ShowIn(tab, node);
        w.History.Push(node, w.Language);
    }

    /// <summary>Resolves <paramref name="reference"/> in the background (it may load an assembly),
    /// then shows it in the tab it was asked from, or a new one.</summary>
    public static async void Navigate(this Workspace w, CodeReference reference, bool inNewTab)
    {
        if (!reference.IsNavigable)
            return;
        var ticket = w.Intent.Begin();
        var origin = inNewTab ? null : w.Tabs.Active;
        w.Intent.StartResolving(ticket);
        var found = await Background.Run(() => w.Assemblies.FindNode(reference));
        if (w.Intent.IsStale(ticket))
            return;
        w.Intent.StopResolving();
        w.ShowResolved(reference, found, origin);
    }

    private static void ShowResolved(this Workspace w, CodeReference reference, Outcome<BrowseNode?> found, DocumentTab? origin)
    {
        var node = found.Value;
        if (node == null)
        {
            w.ReportUnresolved(reference, found.Error);
            return;
        }
        w.LeaveActive();
        w.ShowIn(w.Tabs.KeepIfOpen(origin), node);
        w.History.Push(node, w.Language);
    }

    /// <summary>Shows the table row a metadata token leads to in the active tab, or a new one, and
    /// records it in the history.</summary>
    public static void Show(this Workspace w, MetadataTarget target, bool inNewTab)
    {
        w.Intent.Begin();
        w.LeaveActive();
        var tab = inNewTab ? null : w.Tabs.Active;
        w.ShowIn(tab, target.Node, row: target.Row);
        w.History.Push(target.Node, w.Language);
    }

    /// <summary>Goes where a node points (a base type, an assembly reference), if anywhere.</summary>
    public static void GoToTarget(this Workspace w, BrowseNode node)
    {
        if (node.Target == null)
            return;
        w.Navigate(node.Target, inNewTab: false);
    }

    public static void GoBack(this Workspace w) => w.GoTo(w.History.CurrentIndex - 1);

    public static void GoForward(this Workspace w) => w.GoTo(w.History.CurrentIndex + 1);

    /// <summary>Goes to the history entry at <paramref name="index"/>, however far back or forward.</summary>
    public static void GoTo(this Workspace w, int index)
    {
        if (!w.CanGoTo(index))
            return;
        w.LeaveActive();
        var entry = w.History.MoveTo(index);
        if (entry == null)
            return;
        w.Restore(entry.Value);
    }

    private static bool CanGoTo(this Workspace w, int index)
    {
        if (index < 0)
            return false;
        if (index >= w.History.Entries.Count)
            return false;
        return index != w.History.CurrentIndex;
    }

    private static async void Restore(this Workspace w, HistoryEntry entry)
    {
        var ticket = w.Intent.Begin();
        var tab = w.Tabs.Active;
        var place = entry.Place;
        var position = entry.Language == w.Language ? entry.Position : null;
        var shown = w.FindShownNode(tab, place);
        if (shown != null)
        {
            w.ShowIn(tab, shown, position);
            return;
        }
        w.Intent.StartResolving(ticket);
        var found = await Background.Run(() => w.Assemblies.FindNode(place));
        if (w.Intent.IsStale(ticket))
            return;
        w.Intent.StopResolving();
        w.ShowHistoryPlace(place, position, found, tab);
    }

    private static BrowseNode? FindShownNode(this Workspace w, DocumentTab? tab, NodePath place)
    {
        if (tab == null)
            return null;
        var node = tab.Node;
        if (node == null)
            return null;
        if (!place.Equals(tab.Location))
            return null;
        if (!w.IsListed(node))
            return null;
        return node;
    }

    private static void ShowHistoryPlace(this Workspace w, NodePath place, CodeViewPosition? position, Outcome<BrowseNode?> found, DocumentTab? tab)
    {
        var node = found.Value;
        if (node == null)
        {
            var t = w.GetStrings();
            var details = Workspace.DescribeFailure(found.Error, t.ErrorNoLongerThere(place));
            w.ReportError(t.ErrorGoBack(place.Text), details);
            return;
        }
        w.ShowIn(w.Tabs.KeepIfOpen(tab), node, position);
    }

    internal static void ReportUnresolved(this Workspace w, CodeReference reference, Exception? error)
    {
        var t = w.GetStrings();
        var details = Workspace.DescribeFailure(error, t.ErrorNoDefinition(reference));
        w.ReportError(t.ErrorResolve(reference.Name), details);
    }

    /// <summary>Records where the active tab's view is in the history entry it shows, before it shows
    /// something else.</summary>
    internal static void LeaveActive(this Workspace w)
    {
        var tab = w.Tabs.Active;
        if (tab == null)
            return;
        var position = tab.Position;
        if (position == null)
            return;
        if (!w.IsCurrentHistoryPlace(tab.Location))
            return;
        w.History.Leave(tab.Language, position.Value);
    }

    private static bool IsCurrentHistoryPlace(this Workspace w, NodePath? place)
    {
        var current = w.History.Current;
        if (current == null)
            return false;
        return current.Value.Place.Equals(place);
    }

    /// <summary>Shows <paramref name="node"/> in <paramref name="tab"/>, or a new tab when null, makes
    /// that tab active and reveals the node in the tree.</summary>
    internal static void ShowIn(
        this Workspace w,
        DocumentTab? tab,
        BrowseNode node,
        CodeViewPosition? position = null,
        Bookmark? target = null,
        int row = MetadataGridState.None)
    {
        var shownIn = w.ActivateOrOpenTab(tab);
        w.Tabs.MakeActive(shownIn);
        shownIn.Show(node, w.Language, position, target, row);
        w.Tree.Reveal(node);
    }

    private static DocumentTab ActivateOrOpenTab(this Workspace w, DocumentTab? tab)
    {
        if (tab == null)
            return w.OpenNewTab();
        w.Layout.Activate(tab.Panel);
        return tab;
    }

    private static DocumentTab OpenNewTab(this Workspace w)
    {
        var tab = w.AddTab();
        w.MainLayout.DockDocument(tab.Panel, w.Tabs.ActivePanel);
        return tab;
    }
}
