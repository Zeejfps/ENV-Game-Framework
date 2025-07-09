namespace ZnvQuadTree;

public sealed class RectFQuadTree<T> where T : notnull
{
    private readonly Node<T> _root;

    public int Count => _items.Count;

    private readonly Dictionary<T, RectF> _items = new();

    public RectFQuadTree(RectF bounds)
    {
        _root = new Node<T>(10, bounds);
    }

    public void Insert(T item, RectF bounds)
    {
        if (_items.TryAdd(item, bounds))
        {
            _root.Insert(new BoundedItem<T>(bounds, item));
        }
    }

    public void Remove(T item)
    {
        if (_items.Remove(item, out var bounds))
        {
            var boundedItem = new BoundedItem<T>(bounds, item);
            _root.Remove(boundedItem);
        }
    }

    public bool Contains(T item)
    {
        return _items.ContainsKey(item);
    }
    
    public int Query(RectF searchArea, List<T> results)
    {
        return _root.Query(searchArea, results);
    }

    public IEnumerable<T> Query(RectF searchArea)
    {
        var results = new List<T>();
        Query(searchArea, results);
        return results;
    }

    public void Clear()
    {
        _root.Clear();
        _items.Clear();
    }
}

public readonly struct RectF : IEquatable<RectF>
{
    public RectF(float left, float bottom, float width, float height)
    {
        Left = left;
        Bottom = bottom;
        Width = width;
        Height = height;
        Top = bottom + height;
        Right = left + width;
    }

    public float Left { get; }
    public float Bottom { get; }
    public float Top { get; }
    public float Right { get; }
    public float Width { get; }
    public float Height { get;  }

    public bool Overlaps(RectF otherRect)
    {
        throw new NotImplementedException();
    }

    public bool Contains(RectF otherRect)
    {
        throw new NotImplementedException();
    }

    public bool Equals(RectF other)
    {
        return Left.Equals(other.Left) && Bottom.Equals(other.Bottom) && Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    public override bool Equals(object? obj)
    {
        return obj is RectF other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Left, Bottom, Width, Height);
    }

    public static bool operator ==(RectF left, RectF right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RectF left, RectF right)
    {
        return !left.Equals(right);
    }
}

internal readonly struct BoundedItem<T>(RectF bounds, T item) : IEquatable<BoundedItem<T>>
{
    public readonly RectF Bounds = bounds;
    public readonly T Item = item;

    public bool Equals(BoundedItem<T> other)
    {
        return EqualityComparer<T>.Default.Equals(Item, other.Item);
    }

    public override bool Equals(object? obj)
    {
        return obj is BoundedItem<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(Item!);
    }

    public static bool operator ==(BoundedItem<T> left, BoundedItem<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BoundedItem<T> left, BoundedItem<T> right)
    {
        return !left.Equals(right);
    }
}

internal sealed class Node<T>(int maxItems, RectF bounds)
{
    private readonly List<BoundedItem<T>> _children = new();
    private Node<T>[]? _quads;

    public int ItemCount
    {
        get
        {
            var itemCount = _children.Count;
            if (_quads != null)
            {
                itemCount += CountItems(_quads);
            }
            return itemCount;
        }
    }
    public RectF Bounds => bounds;

    public int Query(RectF searchArea, List<T> results)
    {
        var foundCount = 0;
        if (searchArea.Overlaps(bounds))
        {
            foreach (var item in _children)
            {
                var itemBounds = item.Bounds;
                if (searchArea.Overlaps(itemBounds))
                {
                    results.Add(item.Item);
                    foundCount++;
                }
            }

            if (_quads != null)
            {
                foreach (var quad in _quads)
                {
                    foundCount += quad.Query(searchArea, results);
                }
            }
        }
        return foundCount;
    }

    public void Insert(BoundedItem<T> item)
    {
        var childCount = _children.Count;
        var quads = _quads;
        if (childCount >= maxItems && quads == null)
        {
            quads = Split();
        }
        
        if (quads != null)
        {
            var inserted = InsertIntoQuads(quads, item);
            if (!inserted)
            {
                _children.Add(item);
            }
        }
        else
        {
            _children.Add(item);
        }
    }

    public bool Remove(BoundedItem<T> item)
    {
        if (!bounds.Contains(item.Bounds))
            return false;

        if (_quads != null)
        {
            foreach (var quad in _quads)
            {
                if (quad.Remove(item))
                {
                    var remainingItemCount = CountItems(_quads);
                    if (remainingItemCount == 0)
                    {
                        _quads = null;
                    }
                    return true;
                }
            }
        }
        
        return _children.Remove(item);
    }

    private int CountItems(Node<T>[] quads)
    {
        var itemCount = 0;
        foreach (var quad in quads)
        {
            itemCount += quad.ItemCount;
        }
        return itemCount;
    }
    
    private bool InsertIntoQuads(Node<T>[] quads, BoundedItem<T> item)
    {
        var itemBounds = item.Bounds;
        foreach (var quad in quads)
        {
            if (quad.Bounds.Contains(itemBounds))
            {
                quad.Insert(item);
                return true;
            }
        }
        return false;
    }
    
    private Node<T>[] Split()
    {
        var quadWidth = bounds.Width * 0.5f;
        var quadHeight = bounds.Height * 0.5f;
        
        var topLeftBounds = new RectF(
            left: bounds.Left,
            bottom: bounds.Bottom + quadWidth,
            width: quadWidth,
            height: quadHeight
        );
        var topLeft = new Node<T>(maxItems, topLeftBounds);

        var topRightBounds = new RectF(
            left: bounds.Left + quadWidth,
            bottom: bounds.Bottom + quadHeight,
            width: quadWidth,
            height: quadHeight
        );
        var topRight = new Node<T>(maxItems, topRightBounds);
        
        var botLeftBounds = new RectF(
            left: bounds.Left,
            bottom: bounds.Bottom,
            width: quadWidth,
            height: quadHeight
        );
        var botLeft = new Node<T>(maxItems, botLeftBounds);
        
        var botRightBounds = new RectF(
            left: bounds.Left + quadWidth,
            bottom: bounds.Bottom,
            width: quadWidth,
            height: quadHeight
        );;
        var botRight = new Node<T>(maxItems, botRightBounds);

        _quads =
        [
            topLeft,
            topRight,
            botLeft,
            botRight
        ];
        
        var itemsToRemove = new List<BoundedItem<T>>();
        foreach (var item in _children)
        {
            var inserted = InsertIntoQuads(_quads, item);
            if (inserted)
            {
                itemsToRemove.Add(item);
            }
        }

        foreach (var item in itemsToRemove)
        {
            _children.Remove(item);
        }

        return _quads;
    }

    public void Clear()
    {
        _children.Clear();
        _quads = null;
    }
}