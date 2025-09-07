using System.Diagnostics;
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

public readonly record struct ItemPosition
{
    public required GridPoint Origin { get; init; }
    public required ItemSize Size { get; init; }
}

public sealed class GridStorage<TItem>(int width, int height)
{
    private readonly Dictionary<GridPoint, OccupiedSlot<TItem>> _occupiedSlotsByPoint = new();
    private readonly HashSet<TItem> _items = new();
    
    public bool TryGet(uint x, uint y, [NotNullWhen(true)] out TItem? item, out ItemPosition itemPosition)
    {
        return TryGet(new GridPoint{X = x, Y = y}, out item, out itemPosition);
    }
    
    public bool TryGet(GridPoint point, [NotNullWhen(true)] out TItem? item, out ItemPosition itemPosition)
    {
        if (!_occupiedSlotsByPoint.TryGetValue(point, out var occupiedSlot))
        {
            item = default;
            itemPosition = default;
            return false;
        }
        
        item = occupiedSlot.Item;
        Debug.Assert(item != null);
        itemPosition = new ItemPosition
        {
            Origin = occupiedSlot.Origin,
            Size = occupiedSlot.Size
        };
        return true;
    }

    public bool Contains(TItem item)
    {
        return _items.Contains(item);
    }
    
    public bool TryInsert(int x, int y, int width, int height, TItem item)
    {
        throw new NotImplementedException();
    }
    
    public bool TryInsert(ItemPosition position, TItem item)
    {
        var origin = position.Origin;
        var size = position.Size;

        var slot = new OccupiedSlot<TItem>
        {
            Origin = origin,
            Size = size,
            Item = item
        };
        for (var i = origin.Y; i < size.Height; i++)
        {
            for (var j = origin.X; j < size.Width; j++)
            {
                var insertionPoint = new GridPoint{X = j, Y = i};
                _occupiedSlotsByPoint[insertionPoint] = slot;
            }
        }
        _items.Add(item);
        
        return false;
    }
}

internal sealed class OccupiedSlot<TItem>
{
    public required GridPoint Origin { get; set; }
    public required ItemSize Size { get; set; }
    public required TItem Item { get; set; }
}