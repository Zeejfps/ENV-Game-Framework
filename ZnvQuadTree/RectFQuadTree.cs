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

    public bool Intersects(RectF otherRect)
    {
        if (Right <= otherRect.Left || Left >= otherRect.Right)
            return false; 

        if (Top <= otherRect.Bottom || Bottom >= otherRect.Top)
            return false;

        return true;
    }

    public bool FullyContains(RectF otherRect)
    {
        return otherRect.Left >= Left &&
               otherRect.Right <= Right &&
               otherRect.Bottom >= Bottom &&
               otherRect.Top <= Top;
    }

    public bool ContainsPoint(float x, float y)
    {
        return x >= Left && x < Right &&
               y >= Bottom && y < Top;
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

internal struct Node<T>(int maxItems, RectF bounds)
{
    private readonly List<BoundedItem<T>> _children = new();
    private Node<T>[]? _quads;

    private int ItemCount { get; set; }
    private RectF Bounds => bounds;

    public int Query(RectF searchArea, List<T> results)
    {
        var foundCount = 0;
        if (searchArea.Intersects(bounds))
        {
            foreach (var item in _children)
            {
                var itemBounds = item.Bounds;
                if (searchArea.Intersects(itemBounds))
                {
                    results.Add(item.Item);
                    foundCount++;
                }
            }

            if (_quads != null)
            {
                for (var i = 0; i < _quads.Length; i++)
                {
                    ref var quad = ref _quads[i];
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

        ItemCount++;
    }

    public bool Remove(BoundedItem<T> item)
    {
        if (!bounds.FullyContains(item.Bounds))
            return false;

        if (_quads != null)
        {
            for (var i = 0; i < _quads.Length; i++)
            {
                ref var quad = ref _quads[i]; 
                if (quad.Remove(item))
                {
                    ItemCount -= 1;
                    var remainingItemCount = CountItems(_quads);
                    if (remainingItemCount == 0)
                    {
                        _quads = null;
                    }
                    return true;
                }
            }
        }

        if (_children.Remove(item))
        {
            ItemCount -= 1;
            return true;
        }

        return false;
    }

    private int CountItems(Node<T>[] quads)
    {
        var itemCount = 0;
        for (var i = 0; i < quads.Length; i++)
        {
            ref var quad = ref quads[i];
            itemCount += quad.ItemCount;
        }
        return itemCount;
    }
    
    private bool InsertIntoQuads(Node<T>[] quads, BoundedItem<T> item)
    {
        var itemBounds = item.Bounds;
        for (var i = 0; i < quads.Length; i++)
        {
            ref var quad = ref quads[i];
            if (quad.Bounds.FullyContains(itemBounds))
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
            bottom: bounds.Bottom + quadHeight,
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
        );
        var botRight = new Node<T>(maxItems, botRightBounds);

        _quads =
        [
            topLeft,
            topRight,
            botLeft,
            botRight
        ];
        
        for (var i = _children.Count - 1; i >= 0; i--)
        {
            var item = _children[i];
            var inserted = InsertIntoQuads(_quads, item);
            if (inserted)
            {
                _children.RemoveAt(i);
            }
        }

        return _quads;
    }

    public void Clear()
    {
        _children.Clear();
        _quads = null;
        ItemCount = 0;
    }
}