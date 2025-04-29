namespace NodeGraphApp;

public class Column
{
    public Action<ScreenRect>? BoundsChanged { get; set; }
    public ScreenRect Bounds { get; set; }
    public Padding Padding { get; set; }
    public float ItemGap { get; set; }
    public List<ColumnItem> Items { get; } = new();

    public void DoLayout()
    {
        var totalHeight = ItemGap * (Items.Count - 1) + Padding.Top + Padding.Bottom;
        foreach (var item in Items)
        {
            totalHeight += item.Bounds.Height;
        }
        
        var top = Bounds.Top - Padding.Top;
        var left = Bounds.Left + Padding.Left;
        var width = Bounds.Width - Padding.Left - Padding.Right;
        foreach (var item in Items)
        {
            var height = item.Bounds.Height;
            item.Bounds = ScreenRect.FromLeftTopWidthHeight(left, top, width, height);
            top -= height + ItemGap;
        }
        
        Bounds = Bounds with
        {
            Height = totalHeight
        };
        
        BoundsChanged?.Invoke(Bounds);
    }
}

public sealed class ColumnItem
{
    private ScreenRect _bounds;
    public ScreenRect Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds == value)
                return;
            _bounds = value;
            BoundsChanged?.Invoke(_bounds);
        }
    }
    
    public Action<ScreenRect> BoundsChanged { get; set; }
}