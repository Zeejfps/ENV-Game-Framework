using System.Diagnostics.CodeAnalysis;
using Module.GridStorage.Tests;

namespace Module.GridStorage;

public readonly record struct GridPoint
{
    public required uint X { get; init; }
    public required uint Y { get; init; }
}

public readonly record struct ItemSize
{
    public required uint Width { get; init; }
    public required uint Height { get; init; }
}

public readonly record struct Slot<TItem>
{
    public required GridPoint Origin { get; init; }
    public required ItemSize Size { get; init; }
    public required TItem Item { get; init; }
}

public sealed class GridStorage<TItem>(int width, int height) where TItem : notnull
{
    private readonly Dictionary<GridPoint, Slot<TItem>> _slotsByPointLookup = new();
    private readonly Dictionary<TItem, Slot<TItem>> _slotsByItemLookup = new();

    public bool TryGetItem(uint x, uint y, out TItem item)
    {
        return TryGetItem(new GridPoint { X = x, Y = y }, out item);
    }
    
    public bool TryGetItem(GridPoint point, [MaybeNullWhen(false)] out TItem item)
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
        return TryGetSlot(new GridPoint{X = x, Y = y}, out slot);
    }
    
    public bool TryGetSlot(GridPoint point, out Slot<TItem> slot)
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
    
    public bool TryInsert(uint x, uint y, uint width, uint height, TItem item)
    {
        var origin = new GridPoint { X = x, Y = y };
        var size = new ItemSize { Width = width, Height = height };
        return TryInsert(origin, size, item);
    }
    
    public bool TryInsert(GridPoint origin, ItemSize size, TItem item)
    {
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
                var point = new GridPoint{X = x, Y = y};
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
                var point = new GridPoint{X = x, Y = y};
                _slotsByPointLookup.Remove(point);
            }
        }
    }
}