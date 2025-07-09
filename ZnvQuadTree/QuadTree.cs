namespace ZnvQuadTree;

public sealed class QuadTree<T> where T : notnull
{
    public int ItemCount => _items.Count;

    private readonly Node<T> _root;
    private readonly Dictionary<T, PointF> _items = new();

    public QuadTree(RectF bounds, int maxItemsPerQuad)
    {
        if (maxItemsPerQuad < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItemsPerQuad), "Max items per quad must be at least 1.");
        }
        _root = new Node<T>(maxItemsPerQuad, bounds);
    }

    public void Insert(T item, PointF position)
    {
        if (_items.TryAdd(item, position))
        {
            _root.Insert(new PositionedItem<T>(position, item));
        }
    }

    public void Remove(T item)
    {
        if (_items.Remove(item, out var bounds))
        {
            var boundedItem = new PositionedItem<T>(bounds, item);
            _root.Remove(boundedItem);
        }
    }

    public bool Contains(T item)
    {
        return _items.ContainsKey(item);
    }
    
    public void QueryNonAlloc(RectF searchArea, List<T> results)
    {
        Node<T>.QueryNonAlloc(_root, searchArea, results);
    }

    public IEnumerable<T> Query(RectF searchArea)
    {
        return Node<T>.Query(_root, searchArea);
    }

    public void Clear()
    {
        _root.Clear();
        _items.Clear();
    }
    
    private sealed class Node<T> where T : notnull
    {
        private readonly List<PositionedItem<T>> _children;
        private readonly int _maxItems;
        private readonly RectF _bounds;

        private int _itemCount;
        private Node<T>[]? _quads;
        
        public Node(int maxItems, RectF bounds)
        {
            _maxItems = maxItems;
            _bounds = bounds;
            _children = new List<PositionedItem<T>>();
        }

        public void QueryNonAlloc(RectF searchArea, List<T> results)
        {
            var bounds = _bounds;
            if (searchArea.Intersects(bounds))
            {
                foreach (var item in _children)
                {
                    var position = item.Position;
                    if (searchArea.Contains(position))
                    {
                        results.Add(item.Item);
                    }
                }

                if (_quads != null)
                {
                    foreach (var quad in _quads)
                    {
                        quad.QueryNonAlloc(searchArea, results);
                    }
                }
            }
        }
        public IEnumerable<T> Query(RectF searchArea)
        {
            var bounds = _bounds;
            if (searchArea.Intersects(bounds))
            {
                foreach (var child in _children)
                {
                    var itemPoint = child.Position;
                    if (searchArea.Contains(itemPoint))
                    {
                        yield return child.Item;
                    }
                }

                if (_quads != null)
                {
                    foreach (var quad in _quads)
                    {
                        var results = quad.Query(searchArea);
                        foreach (var item in results)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }
        
        public static void QueryNonAlloc(Node<T> root, RectF searchArea, List<T> results)
        {
            var stack = new Stack<Node<T>>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                
                if (!searchArea.Intersects(node._bounds))
                    continue;
                
                foreach (var boundedItem in node._children)
                {
                    if (searchArea.Contains(boundedItem.Position))
                    {
                        results.Add(boundedItem.Item);
                    }
                }

                if (node._quads != null)
                {
                    foreach (var quad in node._quads)
                    {
                        stack.Push(quad);
                    }
                }
            }
        }

        public static IEnumerable<T> Query(Node<T> root, RectF searchArea)
        {
            var stack = new Stack<Node<T>>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
            
                if (!searchArea.Intersects(node._bounds))
                    continue;

                foreach (var boundedItem in node._children)
                {
                    if (searchArea.Contains(boundedItem.Position))
                    {
                        yield return boundedItem.Item;
                    }
                }

                if (node._quads != null)
                {
                    foreach (var quad in node._quads)
                    {
                        stack.Push(quad);
                    }
                }
            }
        }

        public void Insert(PositionedItem<T> item)
        {
            if (_quads != null)
            {
                if (TryInsertIntoQuads(_quads, item))
                {
                    _itemCount++;
                    return;
                }
            }
            
            _children.Add(item);
            _itemCount++;
            
            if (_children.Count > _maxItems && _quads == null)
            {
                Split();
            }
        }

        public bool Remove(PositionedItem<T> item)
        {
            var bounds = _bounds;
            if (!bounds.Contains(item.Position))
                return false;

            if (_quads != null)
            {
                foreach (var quad in _quads)
                {
                    if (quad.Remove(item))
                    {
                        _itemCount -= 1;
                        if (_itemCount < _maxItems)
                        {
                            CollectItemsFromQuads(_quads, _children);
                            _quads = null;
                        }
                        return true;
                    }
                }
            }

            if (_children.Remove(item))
            {
                _itemCount -= 1;
                return true;
            }

            return false;
        }

        private void CollectItems(List<PositionedItem<T>> items)
        {
            items.AddRange(_children);
            if (_quads != null)
            {
                CollectItemsFromQuads(_quads, items);
            }
        }
        
        private void CollectItemsFromQuads(Node<T>[] quads, List<PositionedItem<T>> items)
        {
            foreach (var quad in quads)
            {
                quad.CollectItems(items);
            }
        }
        
        private bool TryInsertIntoQuads(Node<T>[] quads, PositionedItem<T> item)
        {
            var itemPosition = item.Position;
            foreach (var quad in quads)
            {
                if (quad._bounds.Contains(itemPosition))
                {
                    quad.Insert(item);
                    return true;
                }  
            }
            return false;
        }
        
        private void Split()
        {
            var bounds = _bounds;
            var quadWidth = bounds.Width * 0.5f;
            var quadHeight = bounds.Height * 0.5f;
            
            var topLeftBounds = new RectF(
                left: bounds.Left,
                bottom: bounds.Bottom + quadHeight,
                width: quadWidth,
                height: quadHeight
            );
            var topLeft = new Node<T>(_maxItems, topLeftBounds);

            var topRightBounds = new RectF(
                left: bounds.Left + quadWidth,
                bottom: bounds.Bottom + quadHeight,
                width: quadWidth,
                height: quadHeight
            );
            var topRight = new Node<T>(_maxItems, topRightBounds);
            
            var botLeftBounds = new RectF(
                left: bounds.Left,
                bottom: bounds.Bottom,
                width: quadWidth,
                height: quadHeight
            );
            var botLeft = new Node<T>(_maxItems, botLeftBounds);
            
            var botRightBounds = new RectF(
                left: bounds.Left + quadWidth,
                bottom: bounds.Bottom,
                width: quadWidth,
                height: quadHeight
            );
            var botRight = new Node<T>(_maxItems, botRightBounds);

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
                var inserted = TryInsertIntoQuads(_quads, item);
                if (inserted)
                {
                    _children.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            _children.Clear();
            _quads = null;
            _itemCount = 0;
        }
    }
}

public readonly record struct PointF(float X, float Y);

internal readonly record struct PositionedItem<T>(PointF Position, T Item) where T : notnull;