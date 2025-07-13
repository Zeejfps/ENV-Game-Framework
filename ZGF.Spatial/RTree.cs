using System.Diagnostics;
using ZGF.Geometry;

namespace ZnvQuadTree;

internal sealed class RTreeConfig
{
    public int MaxNodeCapacity { get; }

    public RTreeConfig(int maxNodeCapacity)
    {
        MaxNodeCapacity = maxNodeCapacity;
    }
}

public sealed class RTree<T>
{
    private readonly RTreeNode<T> _root;

    public RTree()
    {
        _root = new RTreeNode<T>();
    }

    public void Insert(T data, RectF boundingBox)
    {
        _root.Insert(data, boundingBox);
    }
}

internal sealed class RTreeNode<T>
{
    public RectF BoundingBox { get; }
    public List<RTreeNodeEntry<T>> Entries { get; }
    public bool IsLeaf { get; }

    public RTreeNode()
    {
        Entries = new List<RTreeNodeEntry<T>>();
    }

    public void Insert(T data, RectF itemsBoundingBox)
    {
        if (IsLeaf)
        {
            var entry = new RTreeNodeEntry<T>
            {
                BoundingBox = itemsBoundingBox,
                Data = data,
            };
            Entries.Add(entry);
            return;
        }

        var bestEntry = Entries
            .OrderBy(entry =>
            {
                var entryBoundingBox = entry.BoundingBox;
                var initialArea = entryBoundingBox.Area;
                var mbr = RectF.CreateMinimumBoundingRect(entryBoundingBox, itemsBoundingBox);
                var areaDelta = mbr.Area - initialArea;
                return areaDelta;
            })
            .First();

        var bestEntryNode = bestEntry.Node;
        Debug.Assert(bestEntryNode != null, "Internal node entry should not have a null Node!");
        bestEntryNode.Insert(data, itemsBoundingBox);
    }
}

internal sealed class RTreeNodeEntry<T>
{
    public RTreeNode<T>? Node { get; init; }
    public T? Data { get; init; }
    public required RectF BoundingBox { get; init; }
}