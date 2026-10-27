using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Decompilation;
using static ZGF.ILSpy.Diagrams;
using static ZGF.ILSpy.Docking;

namespace ZGF.ILSpy;

/// <summary>The documents each assembly list keeps open between runs: keeping the open tabs with the
/// open list, reopening a list's tabs as it opens, and showing what they showed again once its assemblies
/// are listed.</summary>
public static class SavedDocuments
{
    /// <summary>Opens the documents the open list kept, in place of the open tabs, each waiting for what it
    /// showed until the list's assemblies are listed; makes the one that was active and the one that was
    /// the preview tab so again; and commands the view to dock them where they were. With none kept, closes
    /// every tab but the active one, which then shows nothing.</summary>
    public static void ReopenDocuments(AppState appState)
    {
        appState.Reopened.Clear();
        var saved = appState.Lists.ActiveDocuments;
        if (saved == null)
        {
            CloseDocuments(appState);
            return;
        }
        ReopenTabs(appState, saved);
    }

    private static void ReopenTabs(AppState appState, DocumentsRecord saved)
    {
        if (saved.Tabs.Count == 0)
        {
            CloseDocuments(appState);
            return;
        }
        CloseAllTabs(appState);
        var reopened = saved.Tabs.ConvertAll(r => ReopenTab(appState, r));
        MakeSavedActive(appState, reopened, saved.Active);
        MakeSavedPreview(appState, reopened, saved.Preview);
        appState.EnqueueCommand(new ViewCommand.ShowReopened(reopened, saved.Layout));
    }

    private static ReopenedTab ReopenTab(AppState appState, DocumentRecord record)
    {
        var tab = appState.AddTab();
        WaitForSaved(tab, record);
        return appState.Reopened.Wait(tab, record);
    }

    private static void WaitForSaved(DocumentTab tab, DocumentRecord record)
    {
        switch (record)
        {
            case NodeDocumentRecord node:
                tab.WaitForNode(node.ToNodePath(), node.Title, node.Language);
                break;
            case DiagramDocumentRecord diagram:
                tab.WaitForDiagram(diagram.Title);
                break;
        }
    }

    /// <summary>Makes the tab reopened from the one docked as <paramref name="panel"/> active, else the first.</summary>
    private static void MakeSavedActive(AppState appState, List<ReopenedTab> reopened, string? panel)
    {
        var active = FindReopened(reopened, panel);
        if (active == null)
            active = reopened[0];
        appState.Tabs.MakeActive(active.Tab);
    }

    private static void MakeSavedPreview(AppState appState, List<ReopenedTab> reopened, string? panel)
    {
        var preview = FindReopened(reopened, panel);
        if (preview == null)
            return;
        appState.Tabs.MakePreview(preview.Tab);
    }

    private static ReopenedTab? FindReopened(List<ReopenedTab> reopened, string? panel)
    {
        if (panel == null)
            return null;
        return reopened.Find(r => r.Record.Panel == panel);
    }

    /// <summary>The open tabs as the settings keep them with the open list: each docked tab showing a node
    /// or a diagram, and each still waiting for what it was reopened to show, with the active and the
    /// preview tab and how their panels are arranged. Null when no tab is kept.</summary>
    internal static DocumentsRecord? SaveDocuments(AppState appState)
    {
        var saved = appState.Tabs
            .Select(t => SaveTab(appState, t))
            .OfType<SavedTab>()
            .ToList();
        if (saved.Count == 0)
            return null;
        var records = saved.ConvertAll(static s => s.Record);
        var active = FindSavedPanel(saved, appState.Tabs.Active);
        var preview = FindSavedPanel(saved, appState.Tabs.Preview);
        var layout = ArrangeSaved(appState.Settings.Layout, records);
        return new DocumentsRecord(records, active, preview, layout);
    }

    private static SavedTab? SaveTab(AppState appState, DocumentTab tab)
    {
        var panel = FindTabPanel(appState, tab);
        var waiting = appState.Reopened.Find(tab);
        if (waiting != null)
            return new SavedTab(tab, KeepWaiting(waiting, panel));
        if (panel == null)
            return null;
        var record = SaveShown(tab, panel);
        if (record == null)
            return null;
        return new SavedTab(tab, record);
    }

    private static TabPanel? FindTabPanel(AppState appState, DocumentTab tab)
    {
        foreach (var panel in appState.TabPanels)
        {
            if (panel.Tab == tab)
                return panel;
        }
        return null;
    }

    /// <summary>What a tab still waiting was saved as, docked as <paramref name="panel"/> says when the
    /// screen docked it.</summary>
    private static DocumentRecord KeepWaiting(DocumentRecord waiting, TabPanel? panel)
    {
        if (panel == null)
            return waiting;
        return waiting with { Panel = panel.Panel };
    }

    /// <summary>What the settings keep of a tab the screen shows: its diagram, or the node at its location;
    /// null for a tab showing neither, such as a report or nothing.</summary>
    private static DocumentRecord? SaveShown(DocumentTab tab, TabPanel panel)
    {
        var diagram = tab.Diagram;
        if (diagram != null)
            return SaveDiagram(tab, diagram, panel);
        var location = tab.Location;
        if (location == null)
            return null;
        var place = location.Segments.ToList();
        return new NodeDocumentRecord(panel.Panel, tab.Title, location.FilePath, place, tab.Language, tab.PositionToReopen);
    }

    private static DiagramDocumentRecord SaveDiagram(DocumentTab tab, TypeDiagram diagram, TabPanel panel)
    {
        var types = diagram.Cards
            .Select(static c => c.Anchor)
            .ToList();
        var homes = diagram.HomeAssemblies.ToList();
        return new DiagramDocumentRecord(panel.Panel, tab.Title, types, homes, panel.Diagram);
    }

    private static string? FindSavedPanel(List<SavedTab> saved, DocumentTab? tab)
    {
        var found = saved.Find(s => s.Tab == tab);
        if (found == null)
            return null;
        return found.Record.Panel;
    }

    /// <summary>A copy of <paramref name="layout"/> holding the panels of <paramref name="records"/> alone,
    /// as they are arranged in it; null when there is no layout.</summary>
    private static DockLayout? ArrangeSaved(DockLayout? layout, List<DocumentRecord> records)
    {
        if (layout == null)
            return null;
        var panels = records.ConvertAll(static r => r.Panel);
        var arrangement = layout.CopyArrangement();
        arrangement.Retain(panels.Contains);
        return arrangement;
    }

    /// <summary>Shows again what the reopened tabs still waiting showed, now that the open list's assemblies
    /// are listed: finds their nodes in the background, showing each or that it is gone, and reads the
    /// cards of their diagrams.</summary>
    internal static void ShowReopenedDocuments(AppState appState)
    {
        var nodes = new List<WaitingNode>();
        foreach (var reopened in appState.Reopened.ListWaiting())
            ShowReopened(appState, reopened, nodes);
        FindReopenedNodes(appState, nodes);
    }

    /// <summary>Reads the diagram <paramref name="reopened"/> showed, or adds its node to
    /// <paramref name="nodes"/>, to be found with the others.</summary>
    private static void ShowReopened(AppState appState, ReopenedTab reopened, List<WaitingNode> nodes)
    {
        switch (reopened.Record)
        {
            case NodeDocumentRecord node:
                nodes.Add(new WaitingNode(reopened, node, FindListedPlace(appState, node)));
                break;
            case DiagramDocumentRecord diagram:
                ReopenDiagram(appState, reopened, diagram);
                break;
        }
    }

    /// <summary>Where <paramref name="node"/> was, when its file is open in the list; else null, as the
    /// list holds it no more.</summary>
    private static NodePath? FindListedPlace(AppState appState, NodeDocumentRecord node)
    {
        if (!appState.IsListed(node.FilePath))
            return null;
        return node.ToNodePath();
    }

    private static async void FindReopenedNodes(AppState appState, List<WaitingNode> nodes)
    {
        if (nodes.Count == 0)
            return;
        var places = nodes.ConvertAll(static n => n.Place);
        appState.Reopened.StartFinding();
        var found = await Background.Run(() => FindNodes(appState.Assemblies, places));
        appState.Reopened.StopFinding();
        for (var i = 0; i < nodes.Count; i++)
            ShowReopenedNode(appState, nodes[i], FoundAt(found.Value, i));
    }

    private static List<BrowseNode?> FindNodes(AssemblyList list, List<NodePath?> places)
    {
        return places.ConvertAll(p => FindNode(list, p));
    }

    private static BrowseNode? FindNode(AssemblyList list, NodePath? place)
    {
        if (place == null)
            return null;
        return list.FindListedNode(place);
    }

    private static BrowseNode? FoundAt(List<BrowseNode?>? found, int index)
    {
        if (found == null)
            return null;
        return found[index];
    }

    /// <summary>Shows <paramref name="node"/>, found where the waiting tab's node was, back where its view
    /// stood when it was shown in the language the app shows now, or that it is gone; unless the tab
    /// was pointed elsewhere or closed meanwhile.</summary>
    private static void ShowReopenedNode(AppState appState, WaitingNode waiting, BrowseNode? node)
    {
        if (!appState.Reopened.Finish(waiting.Reopened))
            return;
        var tab = waiting.Reopened.Tab;
        if (!appState.Tabs.Contains(tab))
            return;
        if (node == null)
        {
            tab.ShowGone(waiting.Record.ToNodePath());
            return;
        }
        var position = FindSavedPosition(appState, waiting.Record);
        LoadIntoTab(appState, tab, node, new Arrival(position));
        RevealIfActive(appState, tab, node);
    }

    private static CodeViewPosition? FindSavedPosition(AppState appState, NodeDocumentRecord saved)
    {
        if (saved.Language != appState.Language)
            return null;
        return saved.Position;
    }

    private static void RevealIfActive(AppState appState, DocumentTab tab, BrowseNode node)
    {
        if (tab != appState.Tabs.Active)
            return;
        appState.EnqueueCommand(new ViewCommand.RevealNode(node));
    }

    private sealed record SavedTab(DocumentTab Tab, DocumentRecord Record);

    /// <summary>A reopened tab waiting for the node <see cref="Record"/> kept, at <see cref="Place"/> while
    /// its file is listed.</summary>
    private sealed record WaitingNode(ReopenedTab Reopened, NodeDocumentRecord Record, NodePath? Place);
}
