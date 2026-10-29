using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>One step of a <see cref="Breadcrumb"/>: a node on the path and its name as the tree labels it.</summary>
public readonly record struct Crumb(BrowseNode Node, string Label);

/// <summary>The path from the root down to the node a tab shows, as the tree nests it. Built again
/// only when the node or the tree's shape changes.</summary>
public sealed class Breadcrumb
{
    private BrowseNode? _node;
    private int _treeRevision = -1;
    private Crumb[] _crumbs = [];

    public IReadOnlyList<Crumb> Crumbs => _crumbs;

    /// <summary>Follows <paramref name="node"/> as <paramref name="tree"/> shows it; none when it is null.</summary>
    public void Follow(BrowseNode? node, BrowseTree tree)
    {
        if (IsCurrent(node, tree))
            return;
        _node = node;
        _treeRevision = tree.Revision;
        _crumbs = ListCrumbs(node, tree);
    }

    private bool IsCurrent(BrowseNode? node, BrowseTree tree)
    {
        if (!ReferenceEquals(node, _node))
            return false;
        return tree.Revision == _treeRevision;
    }

    private static Crumb[] ListCrumbs(BrowseNode? node, BrowseTree tree)
    {
        if (node == null)
            return [];
        return tree.ListPathTo(node)
            .Select(n => new Crumb(n, LabelOf(n, tree)))
            .ToArray();
    }

    /// <summary>An assembly's name as its tab titles it, with the version that tells apart those of one
    /// name; any other node's as its row labels it.</summary>
    private static string LabelOf(BrowseNode node, BrowseTree tree)
    {
        if (node.Subject is NodeSubject.Module)
            return tree.Label(node);
        return tree.RowLabel(node);
    }
}
