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
