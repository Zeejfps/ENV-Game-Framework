using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>Opening the search panel and keeping its query to an assembly or a namespace.</summary>
public static class Searching
{
    private const string NamespaceScope = "innamespace";
    private const string AssemblyScope = "inassembly";

    /// <summary>Docks the search panel under the documents if it is closed, brings it to the front
    /// and puts the caret in its search box.</summary>
    public static void OpenSearch(this Workspace w)
    {
        w.MainLayout.OpenSearch(w.Tabs.ActivePanel);
        w.IsSearchFocusPending = true;
    }

    /// <summary>Opens the search panel with its query kept to <paramref name="node"/>'s assembly, or to
    /// the namespace it is.</summary>
    public static void ScopeSearch(this Workspace w, BrowseNode node)
    {
        w.Search.Query = w.ScopeQueryTo(node);
        w.OpenSearch();
    }

    private static string ScopeQueryTo(this Workspace w, BrowseNode node)
    {
        if (node.Kind == NodeKind.Namespace)
            return SearchQuery.WithScope(w.Search.Query, NamespaceScope, node.FullName);
        return SearchQuery.WithScope(w.Search.Query, AssemblyScope, node.Assembly.Name);
    }

    /// <summary>True once after <see cref="OpenSearch"/>: the search box should take focus.</summary>
    public static bool TakeSearchFocus(this Workspace w)
    {
        var focus = w.IsSearchFocusPending;
        w.IsSearchFocusPending = false;
        return focus;
    }
}
