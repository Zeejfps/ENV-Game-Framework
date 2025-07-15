using System.Diagnostics.CodeAnalysis;
using ZGF.Geometry;

namespace ZnvQuadTree;

public sealed class QuadTreePointF<T> where T : notnull
{
    private readonly Node _root;
    private readonly Dictionary<T, PointF> _items = new();
    private bool _isDirty;

    public QuadTreePointF(RectF bounds, int maxItemsPerQuad, int maxDepth = 10, float collapseThreshold = 0.75f)
    {
        if (maxItemsPerQuad < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItemsPerQuad), "Max items per quad must be at least 1.");
        }

        if (maxDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "Max depth must be at least 1.");
        }

        if (collapseThreshold <= 0f || collapseThreshold > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(collapseThreshold), "Collapse threshold must be between 0 (exclusive) and 1 (inclusive).");
        }
        
        var collapseItemCount = (int)MathF.Floor(collapseThreshold * maxItemsPerQuad);
        _root = new Node(maxItemsPerQuad, bounds, 0, maxDepth, collapseItemCount);
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
            _isDirty = true;
        }
    }

    public void Update(T item, PointF position)
    {
        if (_items.TryGetValue(item, out var oldPosition))
        {
            var oldCell = new Cell(item, oldPosition);
            _root.Remove(oldCell);
            
            var newCell = new Cell(item, position);
            _root.Insert(newCell);
            _items[item] = position;
        }
        else
        {
            Insert(item, position);
        }

        _isDirty = true;
    }

    public void Remove(T item)
    {
        if (_items.Remove(item, out var bounds))
        {
            var boundedItem = new Cell(item, bounds);
            _root.Remove(boundedItem);
            _isDirty = true;
        }
    }

    public bool Contains(T item)
    {
        return _items.ContainsKey(item);
    }
    
    public void FindInRectNonAlloc(RectF searchArea, List<T> results)
    {
        _root.QueryItemsNonAlloc(searchArea, results);
    }

    public IEnumerable<T> FindInRect(RectF searchArea)
    {
        return Node
            .QueryCell(_root, searchArea)
            .Select(contents => contents.Item);
    }
    
    public IEnumerable<T> FindNearestN(PointF center, int nItems, float maxRadius = float.MaxValue, Predicate<T>? predicate = null)
    {
        if (nItems <= 0)
        {
            return [];
        }
        return _root.FindNearestN(nItems, center, maxRadius, predicate);
    }
    
    
    public void FindInCircleNonAlloc(PointF center, float radius, List<T> items)
    {
        var searchArea = new RectF(
            center.X - radius, 
            center.Y - radius, 
            radius * 2, 
            radius * 2
        );
        
        using var cellQueryCache = ListPool<Cell>.Shared.Rent();
        _root.QueryCellsNonAlloc(searchArea, cellQueryCache.List);

        var radiusSq = radius * radius;
        foreach (var cell in cellQueryCache.List)
        {
            var pos = cell.Position;
            var dx = pos.X - center.X;
            var dy = pos.Y - center.Y;
            if (dx * dx + dy * dy <= radiusSq)
            {
                items.Add(cell.Item);
            }
        }
    }
    
    public IEnumerable<T> FindInCircle(PointF center, float radius)
    {
        var cellsInCircle = FindCellsInCircle(center, radius);
        return cellsInCircle.Select(cell => cell.Item);
    }
    
    private IEnumerable<Cell> FindCellsInCircle(PointF center, float radius)
    {
        var searchArea = new RectF(
            center.X - radius, 
            center.Y - radius, 
            radius * 2, 
            radius * 2
        );

        var queryCircleResults = Node.QueryCell(_root, searchArea);
        var radiusSq = radius * radius;
        return queryCircleResults.Where(cell =>
        {
            var pos = cell.Position;
            var dx = pos.X - center.X;
            var dy = pos.Y - center.Y;
            return dx * dx + dy * dy <= radiusSq;
        });
    }

    public bool TryFindNearest(PointF center, float radius, [NotNullWhen(true)] out T? closestItem, Predicate<T>? predicate = null)
    {
        if (_root.TryFindNearest(center, radius, out var result, predicate))
        {
            closestItem = result.Value.Item;
            return true;
        }

        closestItem = default;
        return false;
    }

    public void Clear()
    {
        _root.Clear();
        _items.Clear();
        _isDirty = true;
    }

    private Info? _info;
    
    public Info GetInfo()
    {
        if (_info == null || _isDirty)
        {
            var nodes = new List<NodeInfo>();
            var maxDepth = 0;
            _root.CollectNodeInfo(nodes, ref maxDepth);
        
            _info = new Info
            {
                MaxDepthReached = maxDepth,
                TotalNodes = nodes.Count,
                TotalItems = ItemCount,
                LeafNodes = nodes.Count(n => !n.IsSplit),
                InternalNodes = nodes.Count(n => n.IsSplit),
                Nodes = nodes
            };

            _isDirty = false;
        }
        
        return _info;
    }
    
    private sealed class Node
    {
        private readonly List<Cell> _cells;
        private readonly int _collapseItemCount;
        private readonly int _maxItemCount;
        private readonly RectF _bounds;
        private readonly int _depth;
        private readonly int _maxDepth;

        private int _itemCount;
        private Node[]? _quads;
        
        public Node(int maxItemCount, RectF bounds, int depth, int maxDepth, int collapseItemCount)
        {
            _maxItemCount = maxItemCount;
            _bounds = bounds;
            _depth = depth;
            _maxDepth = maxDepth;
            _collapseItemCount = collapseItemCount;
            _cells = new List<Cell>();
        }
        
        public void QueryCellsNonAlloc(RectF searchArea, List<Cell> results)
        {
            var bounds = _bounds;
            if (searchArea.Intersects(bounds))
            {
                if (searchArea.FullyContains(bounds))
                {
                    CollectAllCells(results);
                    return;
                }
                
                foreach (var cell in _cells)
                {
                    var position = cell.Position;
                    if (searchArea.ContainsPoint(position))
                    {
                        results.Add(cell);
                    }
                }

                if (_quads != null)
                {
                    foreach (var quad in _quads)
                    {
                        quad.QueryCellsNonAlloc(searchArea, results);
                    }
                }
            }
        }

        public void QueryItemsNonAlloc(RectF searchArea, List<T> results)
        {
            var bounds = _bounds;
            if (searchArea.Intersects(bounds))
            {
                if (searchArea.FullyContains(bounds))
                {
                    CollectAllItems(results);
                    return;
                }
                
                foreach (var cell in _cells)
                {
                    var position = cell.Position;
                    if (searchArea.ContainsPoint(position))
                    {
                        results.Add(cell.Item);
                    }
                }

                if (_quads != null)
                {
                    foreach (var quad in _quads)
                    {
                        quad.QueryItemsNonAlloc(searchArea, results);
                    }
                }
            }
        }
        
        public bool TryFindNearest(PointF center, float radius, [NotNullWhen(true)] out Cell? nearestCell, Predicate<T>? predicate = null)
        {
            nearestCell = null;

            var radiusSq = radius * radius;
            var shortestDistanceSq = radiusSq;
            var itemFound = false;
        
            var nodesToVisit = new PriorityQueue<Node, float>();

            nodesToVisit.Enqueue(this, _bounds.DistanceSqTo(center));

            while (nodesToVisit.TryDequeue(out var node, out var nodeDistSq))
            {
                if (nodeDistSq > shortestDistanceSq)
                {
                    break; 
                }

                foreach (var cell in node._cells)
                {
                    if (predicate != null && !predicate(cell.Item))
                    {
                        continue;
                    }

                    var distanceSq = cell.Position.DistanceSqTo(center);

                    if (distanceSq < shortestDistanceSq)
                    {
                        shortestDistanceSq = distanceSq;
                        nearestCell = cell;
                        itemFound = true;
                    }
                }
            
                if (node._quads != null)
                {
                    foreach (var childNode in node._quads)
                    {
                        var childDistSq = childNode._bounds.DistanceSqTo(center);
                        if (childDistSq < shortestDistanceSq)
                        {
                            nodesToVisit.Enqueue(childNode, childDistSq);
                        }
                    }
                }
            }

            return itemFound;
        }
        
        public IEnumerable<T> FindNearestN(int nItems, PointF center, float maxRadius = float.MaxValue, Predicate<T>? predicate = null)
        {
            if (nItems <= 0) yield break;
            
            var results = new SortedSet<(float distanceSq, T item)>(new DistanceComparer());
            var maxDistanceSq = maxRadius * maxRadius;
            
            var nodesToVisit = new PriorityQueue<Node, float>();
            nodesToVisit.Enqueue(this, _bounds.DistanceSqTo(center));
            
            while (nodesToVisit.TryDequeue(out var node, out var nodeDistanceSq))
            {
                if (results.Count >= nItems && nodeDistanceSq > results.Max.distanceSq)
                {
                    continue;
                }
                
                foreach (var cell in node._cells)
                {
                    if (predicate != null && !predicate(cell.Item))
                    {
                        continue;
                    }
                    
                    var distanceSq = cell.Position.DistanceSqTo(center);
                    
                    if (distanceSq > maxDistanceSq)
                    {
                        continue;
                    }
                    
                    if (results.Count < nItems)
                    {
                        results.Add((distanceSq, cell.Item));
                    }
                    else if (distanceSq < results.Max.distanceSq)
                    {
                        results.Remove(results.Max);
                        results.Add((distanceSq, cell.Item));
                        
                        maxDistanceSq = Math.Min(maxDistanceSq, results.Max.distanceSq);
                    }
                }
                
                if (_quads != null)
                {
                    foreach (var quad in _quads)
                    {
                        var childDistanceSq = quad._bounds.DistanceSqTo(center);
                        if (results.Count < nItems || childDistanceSq < maxDistanceSq)
                        {
                            nodesToVisit.Enqueue(quad, childDistanceSq);
                        }
                    }
                }
            }
            
            foreach (var (_, item) in results)
            {
                yield return item;
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
                    if (searchArea.ContainsPoint(itemPoint))
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
        
        public static IEnumerable<Cell> QueryCell(Node root, RectF searchArea)
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
                    if (searchArea.ContainsPoint(boundedItem.Position))
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

        public void Insert(Cell cell)
        {
            if (_quads != null)
            {
                if (TryInsertCellIntoQuads(cell, _quads))
                {
                    _itemCount++;
                    return;
                }
            }
            
            _cells.Add(cell);
            _itemCount++;
            
            if (_cells.Count > _maxItemCount &&
                _depth < _maxDepth &&
                _quads == null)
            {
                Split();
            }
        }

        public bool Remove(Cell item)
        {
            var bounds = _bounds;
            if (!bounds.ContainsPoint(item.Position))
                return false;

            if (_quads != null)
            {
                foreach (var quad in _quads)
                {
                    if (quad.Remove(item))
                    {
                        _itemCount -= 1;
                        if (_itemCount < _collapseItemCount)
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
        
        private void CollectAllItems(List<T> items)
        {
            foreach (var cell in _cells)
            {
                items.Add(cell.Item);
            }
            if (_quads != null)
            {
                foreach (var quad in _quads)
                {
                    quad.CollectAllItems(items);
                }
            }
        }
        
        private void CollectAllCells(List<Cell> cells)
        {
            cells.AddRange(_cells);
            if (_quads != null)
            {
                CollectCellsFromQuads(_quads, cells);
            }
        }
        
        private void CollectCellsFromQuads(Node[] quads, List<Cell> cells)
        {
            foreach (var quad in quads)
            {
                quad.CollectAllCells(cells);
            }
        }
        
        private bool TryInsertCellIntoQuads(Cell cell, Node[] quads)
        {
            var cellPosition = cell.Position;
            foreach (var quad in quads)
            {
                if (quad._bounds.ContainsPoint(cellPosition))
                {
                    quad.Insert(cell);
                    return true;
                }  
            }
            return false;
        }
        
        private void Split()
        {
            _quads = CreateQuadrants();
            RedistributeCells(_quads);
        }  

        private void RedistributeCells(Node[] quads)
        {
            using var remainingCells = ListPool<Cell>.Shared.Rent();
            foreach (var cell in _cells)
            {
                if (!TryInsertCellIntoQuads(cell, quads))
                {
                    remainingCells.List.Add(cell);
                }
            }
            _cells.Clear();
            _cells.AddRange(remainingCells.List);
        }

        private Node[] CreateQuadrants()
        {
            var bounds = _bounds;
            var quadWidth = bounds.Width * 0.5f;
            var quadHeight = bounds.Height * 0.5f;
            
            var topLeftBounds = new RectF{
                Left = bounds.Left,
                Bottom = bounds.Bottom + quadHeight,
                Width = quadWidth,
                Height = quadHeight
            };
            var topLeft = CreateChildNode(topLeftBounds);

            var topRightBounds = new RectF{
                Left = bounds.Left + quadWidth,
                Bottom = bounds.Bottom + quadHeight,
                Width = quadWidth,
                Height = quadHeight
            };
            var topRight = CreateChildNode(topRightBounds);
            
            var botLeftBounds = new RectF{
                Left = bounds.Left,
                Bottom = bounds.Bottom,
                Width = quadWidth,
                Height = quadHeight
            };
            var botLeft = CreateChildNode(botLeftBounds);
            
            var botRightBounds = new RectF{
                Left = bounds.Left + quadWidth,
                Bottom = bounds.Bottom,
                Width = quadWidth,
                Height = quadHeight
            };
            var botRight = CreateChildNode(botRightBounds);

            return 
            [
                topLeft,
                topRight,
                botLeft,
                botRight
            ];
        }

        public void Clear()
        {
            _cells.Clear();
            _quads = null;
            _itemCount = 0;
        }
    
        public void CollectNodeInfo(List<NodeInfo> nodes, ref int maxDepth)
        {
            maxDepth = Math.Max(maxDepth, _depth);
        
            var nodeInfo = new NodeInfo
            {
                Depth = _depth,
                Bounds = _bounds,
                DirectItemCount = _cells.Count,
                TotalItemCount = _itemCount,
                IsSplit = _quads != null,
            };
        
            nodes.Add(nodeInfo);
        
            if (_quads != null)
            {
                foreach (var child in _quads)
                {
                    child.CollectNodeInfo(nodes, ref maxDepth);
                }
            }
        }
        
        private Node CreateChildNode(RectF bounds)
        {
            return new Node(_maxItemCount, bounds, _depth + 1, _maxDepth, _collapseItemCount);
        }
    }

    private readonly record struct Cell(T Item, PointF Position);

    public sealed class Info
    {
        public required int MaxDepthReached { get; init; }
        public required int TotalNodes { get; init; }
        public required int TotalItems { get; init; }
        public required int LeafNodes { get; init; }
        public required int InternalNodes { get; init; }
        public required IReadOnlyList<NodeInfo> Nodes { get; init; }
    
        public override string ToString()
        {
            return $"QuadTree Info: Items={TotalItems}, Nodes={TotalNodes} (Leaf={LeafNodes}, Internal={InternalNodes}), MaxDepth={MaxDepthReached}";
        }
    }

    public readonly struct NodeInfo
    {
        public required int Depth { get; init; }
        public required RectF Bounds { get; init; }
        public required int DirectItemCount { get; init; }
        public required int TotalItemCount { get; init; }
        public required bool IsSplit { get; init; }
    
        public override string ToString()
        {
            return $"Node[Depth={Depth}, Items={DirectItemCount}/{TotalItemCount}, Bounds={Bounds}]";
        }
    }
    
    private class DistanceComparer : IComparer<(float distanceSq, T item)>
    {
        public int Compare((float distanceSq, T item) x, (float distanceSq, T item) y)
        {
            var distanceComparison = x.distanceSq.CompareTo(y.distanceSq);
            if (distanceComparison != 0) return distanceComparison;
        
            return x.item.GetHashCode().CompareTo(y.item.GetHashCode());
        }
    }
}

