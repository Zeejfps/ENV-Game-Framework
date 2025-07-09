namespace ZnvQuadTree;

public sealed class QuadTreePointF<T> where T : notnull
{
    private readonly Node _root;
    private readonly Dictionary<T, PointF> _items = new();

    public QuadTreePointF(RectF bounds, int maxItemsPerQuad, int maxDepth = 10)
    {
        if (maxItemsPerQuad < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItemsPerQuad), "Max items per quad must be at least 1.");
        }

        if (maxDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "Max depth must be at least 1.");
        }
        
        _root = new Node(maxItemsPerQuad, bounds, 0, maxDepth);
    }
    
    public int ItemCount => _items.Count;

    public IEnumerable<T> GetAllItems()
    {
        return _items.Keys;
    }

    public bool TryGetPosition(T item, out PointF position)
    {
        return _items.TryGetValue(item, out position);
    }

    public void Insert(T item, PointF position)
    {
        if (_items.TryAdd(item, position))
        {
            _root.Insert(new Cell(item, position));
        }
    }

    public void Update(T item, PointF position)
    {
        if (_items.TryGetValue(item, out var oldPosition))
        {
            var oldBoundedItem = new Cell(item, oldPosition);
            _root.Remove(oldBoundedItem);
        
            _items[item] = position;

            var newBoundedItem = new Cell(item, position);
            _root.Insert(newBoundedItem);
        }
        else
        {
            Insert(item, position);
        }
    }

    public void Remove(T item)
    {
        if (_items.Remove(item, out var bounds))
        {
            var boundedItem = new Cell(item, bounds);
            _root.Remove(boundedItem);
        }
    }

    public bool Contains(T item)
    {
        return _items.ContainsKey(item);
    }
    
    public void QueryRectNonAlloc(RectF searchArea, List<T> results)
    {
        Node.QueryNonAlloc(_root, searchArea, results);
    }

    public IEnumerable<T> QueryRect(RectF searchArea)
    {
        return Node
            .Query(_root, searchArea)
            .Select(contents => contents.Item);
    }
    
    public void QueryCircleNonAlloc(PointF center, float radius, List<T> results)
    {
        var searchArea = new RectF(
            center.X - radius, 
            center.Y - radius, 
            radius * 2, 
            radius * 2
        );
    
        QueryRectNonAlloc(searchArea, results);
    
        var radiusSq = radius * radius;
        for (var i = results.Count - 1; i >= 0; i--)
        {
            if (_items.TryGetValue(results[i], out var pos))
            {
                var dx = pos.X - center.X;
                var dy = pos.Y - center.Y;
                if (dx * dx + dy * dy > radiusSq)
                {
                    results.RemoveAt(i);
                }
            }
        }
    }
    
    public IEnumerable<T> QueryCircle(PointF center, float radius)
    {
        var searchArea = new RectF(
            center.X - radius, 
            center.Y - radius, 
            radius * 2, 
            radius * 2
        );

        var rectQueryResults = Node.Query(_root, searchArea);
        var radiusSq = radius * radius;
        return rectQueryResults.Where(cell =>
        {
            var pos = cell.Position;
            var dx = pos.X - center.X;
            var dy = pos.Y - center.Y;
            return dx * dx + dy * dy <= radiusSq;
        }).Select(cell => cell.Item);
    }

    public void Clear()
    {
        _root.Clear();
        _items.Clear();
    }
    
    private sealed class Node
    {
        private readonly List<Cell> _cells;
        private readonly int _maxItems;
        private readonly RectF _bounds;
        private readonly int _depth;
        private readonly int _maxDepth;

        private int _itemCount;
        private Node[]? _quads;
        
        public Node(int maxItems, RectF bounds, int depth, int maxDepth)
        {
            _maxItems = maxItems;
            _bounds = bounds;
            _depth = depth;
            _maxDepth = maxDepth;
            _cells = new List<Cell>();
        }

        public void QueryNonAlloc(RectF searchArea, List<T> results)
        {
            var bounds = _bounds;
            if (searchArea.Intersects(bounds))
            {
                if (searchArea.FullyContains(bounds))
                {
                    CollectCellsNonAlloc(results);
                    return;
                }
                
                foreach (var item in _cells)
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
                if (searchArea.FullyContains(bounds))
                {
                    foreach (var item in EnumerateCells())
                    {
                        yield return item.Item;
                    }
                    yield break;
                }
                
                foreach (var child in _cells)
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
        
        public static void QueryNonAlloc(Node root, RectF searchArea, List<T> results)
        {
            var stack = new Stack<Node>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                
                if (!searchArea.Intersects(node._bounds))
                    continue;

                if (searchArea.FullyContains(node._bounds))
                {
                    node.CollectCellsNonAlloc(results);
                    continue;
                }
                
                foreach (var boundedItem in node._cells)
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
        public static IEnumerable<Cell> Query(Node root, RectF searchArea)
        {
            var stack = new Stack<Node>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                if (!searchArea.Intersects(node._bounds))
                {
                    continue;
                }

                if (searchArea.FullyContains(node._bounds))
                {
                    foreach (var cell in node.EnumerateCells())
                    {
                        yield return cell;
                    }
                    
                    continue;
                }
                
                foreach (var boundedItem in node._cells)
                {
                    if (searchArea.Contains(boundedItem.Position))
                    {
                        yield return boundedItem;
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

        public void Insert(Cell item)
        {
            if (_quads != null)
            {
                if (TryInsertIntoQuads(_quads, item))
                {
                    _itemCount++;
                    return;
                }
            }
            
            _cells.Add(item);
            _itemCount++;
            
            if (_cells.Count > _maxItems &&
                _depth < _maxDepth &&
                _quads == null)
            {
                Split();
            }
        }

        public bool Remove(Cell item)
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
                            CollectCellsFromQuads(_quads, _cells);
                            _quads = null;
                        }
                        return true;
                    }
                }
            }

            if (_cells.Remove(item))
            {
                _itemCount -= 1;
                return true;
            }

            return false;
        }

        private IEnumerable<Cell> EnumerateCells()
        {
            foreach (var cell in _cells)
            {
                yield return cell;
            }
            if (_quads != null)
            {
                foreach (var quad in _quads)
                {
                    var cells = quad.EnumerateCells();
                    foreach (var cell in cells)
                    {
                        yield return cell;
                    }
                }
            }
        }
        
        private void CollectCellsNonAlloc(List<T> items)
        {
            foreach (var cell in _cells)
            {
                items.Add(cell.Item);
            }
            if (_quads != null)
            {
                foreach (var quad in _quads)
                {
                    quad.CollectCellsNonAlloc(items);
                }
            }
        }
        
        private void CollectAllCells(List<Cell> items)
        {
            items.AddRange(_cells);
            if (_quads != null)
            {
                CollectCellsFromQuads(_quads, items);
            }
        }
        
        private void CollectCellsFromQuads(Node[] quads, List<Cell> cells)
        {
            foreach (var quad in quads)
            {
                quad.CollectAllCells(cells);
            }
        }
        
        private bool TryInsertIntoQuads(Node[] quads, Cell cell)
        {
            var cellPosition = cell.Position;
            foreach (var quad in quads)
            {
                if (quad._bounds.Contains(cellPosition))
                {
                    quad.Insert(cell);
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
            var depth = _depth + 1;
            
            var topLeftBounds = new RectF(
                Left: bounds.Left,
                Bottom: bounds.Bottom + quadHeight,
                Width: quadWidth,
                Height: quadHeight
            );
            var topLeft = new Node(_maxItems, topLeftBounds, depth, _maxDepth);

            var topRightBounds = new RectF(
                Left: bounds.Left + quadWidth,
                Bottom: bounds.Bottom + quadHeight,
                Width: quadWidth,
                Height: quadHeight
            );
            var topRight = new Node(_maxItems, topRightBounds, depth, _maxDepth);
            
            var botLeftBounds = new RectF(
                Left: bounds.Left,
                Bottom: bounds.Bottom,
                Width: quadWidth,
                Height: quadHeight
            );
            var botLeft = new Node(_maxItems, botLeftBounds, depth, _maxDepth);
            
            var botRightBounds = new RectF(
                Left: bounds.Left + quadWidth,
                Bottom: bounds.Bottom,
                Width: quadWidth,
                Height: quadHeight
            );
            var botRight = new Node(_maxItems, botRightBounds, depth, _maxDepth);

            _quads =
            [
                topLeft,
                topRight,
                botLeft,
                botRight
            ];
            
            for (var i = _cells.Count - 1; i >= 0; i--)
            {
                var cell = _cells[i];
                var inserted = TryInsertIntoQuads(_quads, cell);
                if (inserted)
                {
                    _cells.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            _cells.Clear();
            _quads = null;
            _itemCount = 0;
        }
    }

    public readonly record struct Cell(T Item, PointF Position);
}

