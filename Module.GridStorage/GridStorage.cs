using System.Diagnostics.CodeAnalysis;

namespace Module.GridStorage;

public sealed class GridStorage<TItem>(uint width, uint height) where TItem : notnull
{
    private readonly Dictionary<Point, Slot<TItem>> _slotsByPointLookup = new();
    private readonly Dictionary<TItem, Slot<TItem>> _slotsByItemLookup = new();

    public uint Width => width;
    public uint Height => height;
    
    public bool TryGetItem(uint x, uint y, [MaybeNullWhen(false)] out TItem item)
    {
        return TryGetItem(Point.Of(x, y), out item);
    }
    
    public bool TryGetItem(in Point point, [MaybeNullWhen(false)] out TItem item)
    {
        if (!TryGetSlot(point, out var slot))
        {
            item = default;
            return false;
        }
        item = slot.Item;
        return true;
    }
    
    public bool TryGetSlot(uint x, uint y, out Slot<TItem> slot)
    {
        return TryGetSlot(new Point{X = x, Y = y}, out slot);
    }
    
    public bool TryGetSlot(Point point, out Slot<TItem> slot)
    {
        return _slotsByPointLookup.TryGetValue(point, out slot);
    }

    public bool TryGetSlot(TItem item, out Slot<TItem> slot)
    {
        return _slotsByItemLookup.TryGetValue(item, out slot);
    }
    
    public bool Contains(TItem item)
    {
        return _slotsByItemLookup.ContainsKey(item);
    }

    public bool TryAdd(TItem item, uint itemWidth, uint itemHeight, out Slot<TItem> slot)
    {
        return TryAdd(item, Size.Of(itemWidth, itemHeight), out slot);
    }
    
    public bool TryAdd(TItem item, uint itemWidth, uint itemHeight)
    {
        return TryAdd(item, itemWidth, itemHeight, out _);
    }

    public bool TryAdd(TItem item, in Size size)
    {
        return TryAdd(item, size, out _);
    }
    
    public bool TryAdd(TItem item, in Size size, out Slot<TItem> slot)
    {
        slot = default;
        if (Contains(item))
        {
            return false;
        }
        
        for (uint y = 0; y <= Height - size.Height; y++)
        {
            for (uint x = 0; x <= Width - size.Width; x++)
            {
                var origin = Point.Of(x, y);

                if (!CanInsertItemAt(origin, size))
                {
                    continue;
                }
                
                slot = new Slot<TItem>
                {
                    Origin = origin,
                    Size = size,
                    Item = item
                };
                    
                FillSlot(slot);
                _slotsByItemLookup[item] = slot;
                return true;
            }
        }
    
        return false;
    }
    
    public bool CanInsertItemAt(in Point origin, in Size size)
    {
        var ey = origin.Y + size.Height;
        if (ey >= Height)
        {
            return false;
        }
        
        var ex = origin.X + size.Width;
        if (ex >= Width)
        {
            return false;
        }
        for (var y = origin.Y; y < ey; y++)
        {
            for (var x = origin.X; x < ex; x++)
            {
                var point = Point.Of(x, y);
                if (_slotsByPointLookup.ContainsKey(point))
                    return false;
            }
        }
    
        return true;
    }
    
    public bool TryInsert(TItem item, uint x, uint y, uint itemWidth, uint itemHeight)
    {
        var origin = new Point { X = x, Y = y };
        var size = new Size { Width = itemWidth, Height = itemHeight };
        return TryInsert(item, origin, size);
    }
    
    public bool TryInsert(TItem item, in Point origin, Size size)
    {
        if (Contains(item))
        {
            return false;
        }
        
        if (!CanInsertItemAt(origin, size))
        {
            return false;
        }
        
        var slot = new Slot<TItem>
        {
            Origin = origin,
            Size = size,
            Item = item
        };
        _slotsByItemLookup.Add(item, slot);
        FillSlot(slot);
        return true;
    }

    public bool Remove(TItem item)
    {
        return Remove(item, out _);
    }

    public bool Remove(TItem item, out Slot<TItem> slot)
    {
        if (!_slotsByItemLookup.Remove(item, out slot))
        {
            return false;
        }
        ClearSlot(slot);
        return true;
    }

    private void FillSlot(in Slot<TItem> slot)
    {
        var origin = slot.Origin;
        var size = slot.Size;
        var sy = origin.Y;
        var ey = origin.Y + size.Height;
        var sx = origin.X;
        var ex = origin.X + size.Width;
        for (var y = sy; y < ey; y++)
        {
            for (var x = sx; x < ex; x++)
            {
                var point = Point.Of(x, y);
                _slotsByPointLookup[point] = slot;
            }
        }
    }

    private void ClearSlot(in Slot<TItem> slot)
    {
        var origin = slot.Origin;
        var size = slot.Size;
        var sy = origin.Y;
        var ey = origin.Y + size.Height;
        var sx = origin.X;
        var ex = origin.X + size.Width;
        for (var y = sy; y < ey; y++)
        {
            for (var x = sx; x < ex; x++)
            {
                var point = new Point{X = x, Y = y};
                _slotsByPointLookup.Remove(point);
            }
        }
    }
}