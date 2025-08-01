﻿using ZGF.Geometry;

namespace ZnvQuadTree;

public sealed class QuadTreeRectF<T> where T : notnull
{
    public int ItemCount => _items.Count;

    private readonly Node<T> _root;
    private readonly Dictionary<T, RectF> _items = new();

    public QuadTreeRectF(RectF bounds, int maxItemsPerQuad)
    {
        if (maxItemsPerQuad < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItemsPerQuad), "Max items per quad must be at least 1.");
        }
        _root = new Node<T>(maxItemsPerQuad, bounds);
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

    private int ItemCount { get; set; }
    private RectF Bounds => bounds;

    public void QueryNonAlloc(RectF searchArea, List<T> results)
    {
        if (searchArea.Intersects(bounds))
        {
            foreach (var item in _children)
            {
                var itemBounds = item.Bounds;
                if (searchArea.Intersects(itemBounds))
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
        if (searchArea.Intersects(bounds))
        {
            foreach (var child in _children)
            {
                var itemBounds = child.Bounds;
                if (searchArea.Intersects(itemBounds))
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
            
            if (!searchArea.Intersects(node.Bounds))
                continue;
            
            foreach (var boundedItem in node._children)
            {
                if (searchArea.Intersects(boundedItem.Bounds))
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
        
            if (!searchArea.Intersects(node.Bounds))
                continue;

            foreach (var boundedItem in node._children)
            {
                if (searchArea.Intersects(boundedItem.Bounds))
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

    public void Insert(BoundedItem<T> item)
    {
        if (_quads != null)
        {
            if (TryInsertIntoQuads(_quads, item))
            {
                ItemCount++;
                return;
            }
        }
        
        _children.Add(item);
        ItemCount++;
        
        if (_children.Count > maxItems && _quads == null)
        {
            Split();
        }
    }

    public bool Remove(BoundedItem<T> item)
    {
        if (!bounds.FullyContains(item.Bounds))
            return false;

        if (_quads != null)
        {
            foreach (var quad in _quads)
            {
                if (quad.Remove(item))
                {
                    ItemCount -= 1;
                    if (ItemCount < maxItems)
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
            ItemCount -= 1;
            return true;
        }

        return false;
    }

    private void CollectItems(List<BoundedItem<T>> items)
    {
        items.AddRange(_children);
        if (_quads != null)
        {
            CollectItemsFromQuads(_quads, items);
        }
    }
    
    private void CollectItemsFromQuads(Node<T>[] quads, List<BoundedItem<T>> items)
    {
        foreach (var quad in quads)
        {
            quad.CollectItems(items);
        }
    }
    
    private bool TryInsertIntoQuads(Node<T>[] quads, BoundedItem<T> item)
    {
        var itemBounds = item.Bounds;
        foreach (var quad in quads)
        {
            if (quad.Bounds.FullyContains(itemBounds))
            {
                quad.Insert(item);
                return true;
            }  
        }
        return false;
    }
    
    private void Split()
    {
        var quadWidth = bounds.Width * 0.5f;
        var quadHeight = bounds.Height * 0.5f;
        
        var topLeftBounds = new RectF{
            Left = bounds.Left,
            Bottom = bounds.Bottom + quadHeight,
            Width = quadWidth,
            Height = quadHeight
        };
        var topLeft = new Node<T>(maxItems, topLeftBounds);

        var topRightBounds = new RectF{
            Left = bounds.Left + quadWidth,
            Bottom = bounds.Bottom + quadHeight,
            Width = quadWidth,
            Height = quadHeight
        };
        var topRight = new Node<T>(maxItems, topRightBounds);
        
        var botLeftBounds = new RectF{
            Left = bounds.Left,
            Bottom = bounds.Bottom,
            Width = quadWidth,
            Height = quadHeight
        };
        var botLeft = new Node<T>(maxItems, botLeftBounds);
        
        var botRightBounds = new RectF{
            Left = bounds.Left + quadWidth,
            Bottom = bounds.Bottom,
            Width = quadWidth,
            Height = quadHeight
        };
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
        ItemCount = 0;
    }

    private int CountItems(Node<T>[] quads)
    {
        var count = 0;
        foreach (var quad in quads)
        {
            count += quad.ItemCount;
        }

        return count;
    }
}