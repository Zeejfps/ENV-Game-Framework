using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>Where the main window's panels go: the documents together right of the assemblies, and
/// the other views beside or below them. Methods that place a panel by the documents take the active
/// document's panel, whose group comes first.</summary>
public sealed class MainLayout
{
    private const float ToolShare = 0.3f;
    private const float DocumentShare = 0.66f;
    private const float AssembliesShare = 0.34f;

    public MainLayout(DockLayout dock)
    {
        Dock = dock;
    }

    public DockLayout Dock { get; }

    /// <summary>Docks the analyzer beside the search results or bookmarks, or below the documents,
    /// if it is not docked, and brings it to the front.</summary>
    public void OpenAnalyzer(string? activeDocument)
    {
        if (!Dock.Contains(DockPanels.Analyzer))
            DockAnalyzer(activeDocument);
        Dock.Activate(DockPanels.Analyzer);
    }

    private void DockAnalyzer(string? activeDocument)
    {
        var beside = FindSearchOrBookmarksGroup();
        if (beside != null)
        {
            Dock.Dock(DockPanels.Analyzer, beside, DockEdge.Center);
            return;
        }
        DockBelowDocuments(DockPanels.Analyzer, activeDocument);
    }

    private DockGroup? FindSearchOrBookmarksGroup()
    {
        var search = Dock.GroupOf(DockPanels.Search);
        if (search != null)
            return search;
        return Dock.GroupOf(DockPanels.Bookmarks);
    }

    /// <summary>Docks the decompiler options beside the documents if they are not, and brings them to the front.</summary>
    public void OpenDecompilerOptions(string? activeDocument)
    {
        if (!Dock.Contains(DockPanels.DecompilerOptions))
            DockBeside(DockPanels.DecompilerOptions, FindDocumentArea(activeDocument), DockEdge.Right, ToolShare);
        Dock.Activate(DockPanels.DecompilerOptions);
    }

    /// <summary>Docks the display options beside the decompiler options, or beside the documents, if
    /// they are not, and brings them to the front.</summary>
    public void OpenDisplayOptions(string? activeDocument)
    {
        if (!Dock.Contains(DockPanels.DisplayOptions))
            DockDisplayOptions(activeDocument);
        Dock.Activate(DockPanels.DisplayOptions);
    }

    private void DockDisplayOptions(string? activeDocument)
    {
        var decompilerOptions = Dock.GroupOf(DockPanels.DecompilerOptions);
        if (decompilerOptions != null)
        {
            Dock.Dock(DockPanels.DisplayOptions, decompilerOptions, DockEdge.Center);
            return;
        }
        DockBeside(DockPanels.DisplayOptions, FindDocumentArea(activeDocument), DockEdge.Right, ToolShare);
    }

    /// <summary>Docks the bookmarks panel if it is not, and brings it to the front.</summary>
    public void OpenBookmarks(string? activeDocument)
    {
        if (!Dock.Contains(DockPanels.Bookmarks))
            DockBookmarks(activeDocument);
        Dock.Activate(DockPanels.Bookmarks);
    }

    private void DockBookmarks(string? activeDocument)
    {
        var search = Dock.GroupOf(DockPanels.Search);
        if (search != null)
        {
            Dock.Dock(DockPanels.Bookmarks, search, DockEdge.Center);
            return;
        }
        DockBelowDocuments(DockPanels.Bookmarks, activeDocument);
    }

    /// <summary>Docks the exports panel beside the search results, bookmarks or analyzer, or below
    /// the documents, if it is not docked, and brings it to the front.</summary>
    public void OpenExports(string? activeDocument)
    {
        if (!Dock.Contains(DockPanels.Exports))
            DockExports(activeDocument);
        Dock.Activate(DockPanels.Exports);
    }

    private void DockExports(string? activeDocument)
    {
        var beside = FindToolGroup();
        if (beside != null)
        {
            Dock.Dock(DockPanels.Exports, beside, DockEdge.Center);
            return;
        }
        DockBelowDocuments(DockPanels.Exports, activeDocument);
    }

    private DockGroup? FindToolGroup()
    {
        var group = FindSearchOrBookmarksGroup();
        if (group != null)
            return group;
        return Dock.GroupOf(DockPanels.Analyzer);
    }

    /// <summary>Docks the search panel under the documents if it is closed, and brings it to the front.</summary>
    public void OpenSearch(string? activeDocument)
    {
        if (!Dock.Contains(DockPanels.Search))
            DockBelowDocuments(DockPanels.Search, activeDocument);
        Dock.Activate(DockPanels.Search);
    }

    /// <summary>Docks the assemblies view where it starts out, left of everything, if it is closed,
    /// and brings it to the front.</summary>
    public void OpenAssemblies()
    {
        if (!Dock.Contains(DockPanels.Assemblies))
            DockBeside(DockPanels.Assemblies, Dock.Root, DockEdge.Left, AssembliesShare);
        Dock.Activate(DockPanels.Assemblies);
    }

    /// <summary>Docks a new document into the documents' group, or where documents start out, right
    /// of everything, when there is none.</summary>
    public void DockDocument(string panel, string? activeDocument)
    {
        var group = FindDocumentGroup(activeDocument);
        if (group != null)
        {
            Dock.Dock(panel, group, DockEdge.Center);
            return;
        }
        DockBeside(panel, Dock.Root, DockEdge.Right, DocumentShare);
    }

    /// <summary>Docks <paramref name="panels"/>, documents not docked yet, as <paramref name="arrangement"/>,
    /// a layout of documents named as they are, has them: its first group joins the documents' group, or
    /// goes where documents start out when there is none, the groups it splits off go beside that as it
    /// splits them, and its floating windows float again. Those it does not hold join the documents' group.</summary>
    public void DockArranged(IReadOnlyList<string> panels, DockLayout arrangement)
    {
        var documents = FindDocumentGroup(null);
        DockArranged(arrangement.Root, documents, DockEdge.Center, DocumentShare);
        foreach (var window in arrangement.Windows)
            FloatArranged(window);
        foreach (var panel in panels)
            DockIfUndocked(panel);
    }

    /// <summary>Docks the documents under <paramref name="node"/> as it arranges them: its lead group into or
    /// beside <paramref name="target"/> as <paramref name="edge"/> says, or where documents start out when
    /// there is no target, then the others in the place that lead group takes.</summary>
    private void DockArranged(DockNode node, DockGroup? target, DockEdge edge, float share)
    {
        var lead = FindLeadGroup(node);
        if (lead == null)
            return;
        var docked = DockGroupArranged(lead, target, edge, share);
        if (docked == null)
            return;
        DockRestArranged(node, docked);
    }

    /// <summary>Docks the groups under <paramref name="node"/> but its lead, docked as <paramref name="lead"/>,
    /// splitting the place it takes as <paramref name="node"/> splits it: each split's second side goes beside
    /// its first while that first side still fills the place, so each side then splits only its own part.</summary>
    private void DockRestArranged(DockNode node, DockGroup lead)
    {
        if (node is not DockSplit split)
            return;
        if (FindLeadGroup(split.First) == null)
        {
            DockRestArranged(split.Second, lead);
            return;
        }
        var second = FindLeadGroup(split.Second);
        if (second == null)
        {
            DockRestArranged(split.First, lead);
            return;
        }
        var secondDocked = DockGroupArranged(second, lead, EdgeAfter(split.Direction), 1f - split.Ratio);
        DockRestArranged(split.First, lead);
        if (secondDocked != null)
            DockRestArranged(split.Second, secondDocked);
    }

    /// <summary>The first group under <paramref name="node"/> that holds a document, or null when none does.</summary>
    private static DockGroup? FindLeadGroup(DockNode node)
    {
        if (node is DockGroup group)
            return group.Panels.Count > 0 ? group : null;
        if (node is not DockSplit split)
            return null;
        var first = FindLeadGroup(split.First);
        if (first != null)
            return first;
        return FindLeadGroup(split.Second);
    }

    private DockGroup? DockGroupArranged(DockGroup group, DockGroup? target, DockEdge edge, float share)
    {
        if (group.Panels.Count == 0)
            return null;
        var first = group.Panels[0];
        DockFirstArranged(first, target, edge, share);
        var docked = Dock.GroupOf(first);
        if (docked == null)
            return null;
        foreach (var panel in group.Panels.Skip(1))
            Dock.Dock(panel, docked, DockEdge.Center);
        BringToFront(group.Active);
        return docked;
    }

    /// <summary>Docks <paramref name="panel"/>, the first of a group arranged, into or beside
    /// <paramref name="target"/>, or where documents start out when there is none; unless it is docked
    /// already, as the first panel of a floating window is.</summary>
    private void DockFirstArranged(string panel, DockGroup? target, DockEdge edge, float share)
    {
        if (Dock.Contains(panel))
            return;
        if (target == null)
        {
            DockBeside(panel, Dock.Root, DockEdge.Right, DocumentShare);
            return;
        }
        Dock.Dock(panel, target, edge, share);
    }

    private void BringToFront(string? panel)
    {
        if (panel == null)
            return;
        Dock.Activate(panel);
    }

    private static DockEdge EdgeAfter(Direction direction) => direction switch
    {
        Direction.Horizontal => DockEdge.Right,
        Direction.Vertical => DockEdge.Bottom,
        _ => throw new UnreachableException(),
    };

    /// <summary>Floats the documents of <paramref name="window"/> in a window of their own where it was,
    /// arranged as it arranges them.</summary>
    private void FloatArranged(DockWindow window)
    {
        var panels = DockLayout.PanelsIn(window.Root);
        if (panels.Count == 0)
            return;
        Dock.Float(panels[0], window.Rect);
        DockArranged(window.Root, null, DockEdge.Center, DocumentShare);
    }

    private void DockIfUndocked(string panel)
    {
        if (Dock.Contains(panel))
            return;
        DockDocument(panel, null);
    }

    public List<string> ListDocuments()
    {
        return Dock.AllGroups()
            .SelectMany(g => g.Panels)
            .Where(IsDocument)
            .ToList();
    }

    /// <summary>Puts the layout back as it starts out: the assemblies left of <paramref name="documents"/>,
    /// tabbed together in one group. Floating windows and the other views close.</summary>
    public void Reset(List<string> documents)
    {
        Dock.Windows.Clear();
        Dock.Root = CreateAssembliesGroup();
        DockDocumentsTogether(documents);
    }

    private DockGroup CreateAssembliesGroup()
    {
        var id = Dock.NextId;
        Dock.NextId++;
        return new DockGroup
        {
            Id = id,
            Panels = { DockPanels.Assemblies },
            Active = DockPanels.Assemblies,
        };
    }

    private void DockDocumentsTogether(List<string> documents)
    {
        var first = documents[0];
        DockBeside(first, Dock.Root, DockEdge.Right, DocumentShare);
        var group = Dock.GroupOf(first)!;
        foreach (var panel in documents.Skip(1))
            Dock.Dock(panel, group, DockEdge.Center);
    }

    private void DockBelowDocuments(string panel, string? activeDocument)
    {
        DockBeside(panel, FindDocumentArea(activeDocument), DockEdge.Bottom, ToolShare);
    }

    private DockNode FindDocumentArea(string? activeDocument)
    {
        var group = FindDocumentGroup(activeDocument);
        if (group != null)
            return group;
        return Dock.Root;
    }

    /// <summary>The group a new document joins: the active document's, else the first holding one.</summary>
    private DockGroup? FindDocumentGroup(string? activeDocument)
    {
        var active = FindActiveGroup(activeDocument);
        if (active != null)
            return active;
        foreach (var group in Dock.AllGroups())
        {
            if (group.Panels.Exists(IsDocument))
                return group;
        }
        return null;
    }

    private DockGroup? FindActiveGroup(string? activeDocument)
    {
        if (activeDocument == null)
            return null;
        return Dock.GroupOf(activeDocument);
    }

    /// <summary>Docks <paramref name="panel"/> beside <paramref name="target"/>, or into it when the
    /// layout is empty.</summary>
    private void DockBeside(string panel, DockNode target, DockEdge edge, float share)
    {
        var root = Dock.Root as DockGroup;
        if (root != null && root.Panels.Count == 0)
        {
            Dock.Dock(panel, root, DockEdge.Center);
            return;
        }
        Dock.Dock(panel, target, edge, share);
    }

    private static bool IsDocument(string panel) => !DockPanels.IsView(panel);
}
