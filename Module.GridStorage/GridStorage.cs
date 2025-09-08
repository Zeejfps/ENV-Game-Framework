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

public sealed class GridStorage<TItem>(int width, int height)
{
    private readonly Dictionary<GridPoint, Slot<TItem>> _slotsByPointLookup = new();
    private readonly Dictionary<TItem, Slot<TItem>> _slotsByItemLookup = new();
    
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
        for (var i = origin.Y; i < size.Height; i++)
        {
            for (var j = origin.X; j < size.Width; j++)
            {
                var insertionPoint = new GridPoint{X = j, Y = i};
                _slotsByPointLookup[insertionPoint] = slot;
            }
        }
        _slotsByItemLookup.Add(item, slot);
        return true;
    }
}